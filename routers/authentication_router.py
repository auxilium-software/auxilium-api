import logging
import os
import secrets

from fastapi import FastAPI, HTTPException, Depends, status, APIRouter
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel, EmailStr
from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker
from passlib.context import CryptContext
from passlib.hash import argon2
import jwt
from datetime import datetime, timedelta
from typing import Optional, List
import hashlib
import httpx

from common.captcha_helpers import _verify_recaptcha
from common.couchdb_interactions import get_couchdb_connection
from common.mariadb_interactions import get_mariadb_connection

from common.password_helpers import get_password_hash, verify_password
from common.security_utilities import create_refresh_token, REFRESH_TOKEN_EXPIRE_DAYS, ACCESS_TOKEN_EXPIRE_MINUTES, \
    create_access_token, get_current_user
from common.uuid_handling import UUIDHandling
from enumerators.casbin_role import CasbinRole
from enumerators.database_object_type import DatabaseObjectType
from models.refresh.refresh_request_model import RefreshRequestModel
from models.success_response_model import SuccessResponseModel
from models.user_login.user_login_request_model import UserLoginRequestModel
from models.user_login.user_login_response_model import UserLoginResponseModel
from models.user_registration.user_registration_request_model import UserRegistrationRequestModel
from models.user_registration.user_registration_response_model import UserRegistrationResponseModel

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v3/authentication", tags=["Authentication"])




@router.post("/register", response_model=UserRegistrationResponseModel, status_code=status.HTTP_201_CREATED)
async def register(
        request: UserRegistrationRequestModel,
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
        client_ip: Optional[str] = None
):
    try:
        if hasattr(request, 'recaptcha_token') and request.recaptcha_token:
            await _verify_recaptcha(request.recaptcha_token, client_ip)
        else:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="reCAPTCHA token is required"
            )

        result = mariadb.execute(
            text("SELECT * FROM users WHERE email_address = :email"),
            {"email": request.email_address}
        )
        user = result.fetchone()

        if user:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Email address is already associated with an existing user account."
            )

        user_id = UUIDHandling().v5s(ObjectType=DatabaseObjectType.STANDARD_LOGIN)
        password_hash = get_password_hash(request.raw_password)

        mariadb.execute(
            text("""
                INSERT INTO users (id, email_address, password_hash) 
                VALUES (:user_id, :email_address, :password_hash)
            """),
            {
                "user_id": user_id,
                "email_address": request.email_address,
                "password_hash": password_hash,
            }
        )

        user_doc = {

        }
        case_doc = {
            "description": request.description,
        }


        couchdb

        mariadb.commit()

        return UserRegistrationResponseModel(
            id=user_id,
            email_address=request.email_address,
        )

    except Exception as e:
        mariadb.rollback()
        raise e


@router.post("/login", response_model=UserLoginResponseModel)
async def login(
        request: UserLoginRequestModel,
        mariadb=Depends(get_mariadb_connection),
        client_ip: Optional[str] = None
):
    try:
        if hasattr(request, 'recaptcha_token') and request.recaptcha_token:
            await _verify_recaptcha(request.recaptcha_token, client_ip)
        else:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="reCAPTCHA token is required"
            )

        result = mariadb.execute(
            text(
                "SELECT * FROM standard_logins WHERE email_address = :email"),
            {
                "email": request.email_address,
            }
        )
        user = result.fetchone()

        if not user:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Invalid credentials"
            )

        if not user.allow_login:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Account blocked from logging in by the Auxilium IT department."
            )

        if not verify_password(request.raw_password, user.password_hash):
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Invalid credentials"
            )

        user_data = {
            "id": user.id,
            "primary_email_address": user.primary_email_address,
            "preferred_name": user.preferred_name,
        }
        access_token = create_access_token(user_data)
        refresh_token = create_refresh_token(user_data)

        # Clean up expired refresh tokens
        mariadb.execute(
            text("""
                DELETE FROM refresh_tokens 
                WHERE user_id=:user_id AND expires_at<NOW()
            """),
            {"user_id": user.id}
        )

        # Store refresh token
        token_hash = hashlib.sha256(refresh_token.encode()).hexdigest()
        expires_at = datetime.utcnow() + timedelta(days=REFRESH_TOKEN_EXPIRE_DAYS)

        mariadb.execute(
            text("""
                INSERT INTO refresh_tokens (user_id, token_hash, expires_at) 
                VALUES (:user_id, :token_hash, :expires_at)
            """),
            {
                "user_id": user.id,
                "token_hash": token_hash,
                "expires_at": expires_at
            }
        )
        mariadb.commit()

        return UserLoginResponseModel(
            access_token=access_token,
            refresh_token=refresh_token,
            expires_in=ACCESS_TOKEN_EXPIRE_MINUTES * 60
        )

    except Exception as e:
        mariadb.rollback()
        raise e


@router.post("/refresh", response_model=UserLoginResponseModel)
async def refresh(
        request: RefreshRequestModel,
        mariadb=Depends(get_mariadb_connection)
):
    try:
        token_hash = hashlib.sha256(request.refresh_token.encode()).hexdigest()
        logger.debug(token_hash)

        result = mariadb.execute(
            text("""
                SELECT RT.*, SL.* FROM refresh_tokens AS RT
                INNER JOIN standard_logins AS SL ON RT.user_id = SL.id
                WHERE RT.token_hash = :token_hash AND RT.expires_at > NOW()
            """),
            {
                "token_hash": token_hash,
            }
        )
        token_record = result.fetchone()

        if not token_record:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Invalid or expired refresh token"
            )

        user_data = {
            "id": token_record.user_id,
            "primary_email_address": token_record.primary_email_address,
            "preferred_name": token_record.preferred_name,
        }

        access_token = create_access_token(user_data)
        new_refresh_token = create_refresh_token(user_data)

        new_token_hash = hashlib.sha256(new_refresh_token.encode()).hexdigest()
        new_expires_at = datetime.utcnow() + timedelta(days=REFRESH_TOKEN_EXPIRE_DAYS)

        mariadb.execute(
            text("""
                UPDATE refresh_tokens
                SET token_hash = :new_token_hash, expires_at = :expires_at
                WHERE token_hash = :old_token_hash
            """),
            {
                "new_token_hash": new_token_hash,
                "expires_at": new_expires_at,
                "old_token_hash": token_hash
            }
        )
        mariadb.commit()

        return UserLoginResponseModel(
            access_token=access_token,
            refresh_token=new_refresh_token,
            expires_in=ACCESS_TOKEN_EXPIRE_MINUTES * 60  # Convert minutes to seconds
        )

    except Exception as e:
        mariadb.rollback()
        raise e


@router.post("/logout", response_model=SuccessResponseModel)
async def logout(
        current_user=Depends(get_current_user),
        db=Depends(get_mariadb_connection)
):
    try:
        user_id = current_user["sub"]

        # Remove all refresh tokens for this user
        db.execute(
            text("DELETE FROM refresh_tokens WHERE user_id = :user_id"),
            {
                "user_id": user_id,
            }
        )
        db.commit()

        return SuccessResponseModel()

    except Exception as e:
        db.rollback()
        raise e
