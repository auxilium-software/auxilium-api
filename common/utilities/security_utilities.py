import logging
import os
from datetime import datetime, timedelta

import jwt
from fastapi import HTTPException, Depends, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from passlib.context import CryptContext
from sqlalchemy import text

from common.databases.mariadb_interactions import get_mariadb_connection
from common.utilities.configuration import get_configuration

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

ACCESS_TOKEN_EXPIRE_MINUTES = 30
REFRESH_TOKEN_EXPIRE_DAYS = 7

security = HTTPBearer()
pwd_context = CryptContext(
    schemes=["argon2"],
    argon2__memory_cost=2048,
    argon2__time_cost=4,
    argon2__parallelism=3
)

from collections import defaultdict
import time


class RateLimiter:
    def __init__(self):
        self.attempts = defaultdict(list)
        self.max_attempts = int(os.getenv('MAX_LOGIN_ATTEMPTS', '5'))
        self.window_minutes = int(os.getenv('RATE_LIMIT_WINDOW_MINUTES', '15'))

    def is_rate_limited(self, identifier: str) -> bool:
        now = time.time()
        window_start = now - (self.window_minutes * 60)

        self.attempts[identifier] = [
            attempt_time for attempt_time in self.attempts[identifier]
            if attempt_time > window_start
        ]

        return len(self.attempts[identifier]) >= self.max_attempts

    def record_attempt(self, identifier: str):
        self.attempts[identifier].append(time.time())


rate_limiter = RateLimiter()


def create_access_token(user_data: dict):
    configuration = get_configuration()
    secret_key = configuration.get_string('JWT', 'SecretKey')
    algorithm = configuration.get_string('JWT', 'Algorithm')

    to_encode = user_data.copy()
    expire = datetime.utcnow() + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire, "sub": str(user_data["id"])})
    return jwt.encode(to_encode, secret_key, algorithm=algorithm)


def create_refresh_token(user_data: dict):
    configuration = get_configuration()
    secret_key = configuration.get_string('JWT', 'SecretKey')
    algorithm = configuration.get_string('JWT', 'Algorithm')

    to_encode = user_data.copy()
    expire = datetime.utcnow() + timedelta(days=REFRESH_TOKEN_EXPIRE_DAYS)
    to_encode.update({"exp": expire, "sub": str(user_data["id"])})
    return jwt.encode(to_encode, secret_key, algorithm=algorithm)


def decode_token(token: str):
    configuration = get_configuration()
    secret_key = configuration.get_string('JWT', 'SecretKey')
    algorithm = configuration.get_string('JWT', 'Algorithm')

    try:
        payload = jwt.decode(token, secret_key, algorithms=[algorithm])
        return payload
    except jwt.PyJWTError:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Could not validate credentials"
        )


def get_current_user(
        credentials: HTTPAuthorizationCredentials = Depends(security),
        mariadb=Depends(get_mariadb_connection),
):
    token = credentials.credentials

    try:
        payload = decode_token(token)
        user_id = payload.get("sub")
        if user_id is None:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Could not validate credentials"
            )

        result = mariadb.execute(
            text("SELECT * FROM users WHERE id = :id"),
            {"id": user_id}
        )
        mariadb_data = result.fetchone()

        if mariadb_data is None:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="User does not exist"
            )

        return mariadb_data
    except Exception:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Could not validate credentials"
        )
