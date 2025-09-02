import logging

from fastapi import HTTPException, Depends, status, APIRouter
from sqlalchemy import text
from datetime import datetime, timedelta
from typing import Optional
import hashlib

from common.captcha_helpers import _verify_recaptcha
from common.databases.couchdb_interactions import get_couchdb_connection
from common.databases.mariadb_interactions import get_mariadb_connection

from common.password_helpers import get_password_hash, verify_password
from common.utilities.configuration import get_configuration
from common.utilities.security_utilities import create_refresh_token, REFRESH_TOKEN_EXPIRE_DAYS, ACCESS_TOKEN_EXPIRE_MINUTES, \
    create_access_token, get_current_user
from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType
from models.refresh.refresh_request_model import RefreshRequestModel
from models.success_response_model import SuccessResponseModel
from models.user_login.user_login_request_model import UserLoginRequestModel
from models.user_login.user_login_response_model import UserLoginResponseModel
from models.user_registration.user_registration_request_model import UserRegistrationRequestModel
from models.user_registration.user_registration_response_model import UserRegistrationResponseModel
from models.user.user_details_response_model import UserDetailsResponseModel

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

router = APIRouter(prefix="/api/v3/users", tags=["Users"])


@router.get(
    path="/me",
    response_model=UserDetailsResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Authentication"
    ],
)
async def me(
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        result = mariadb.execute(
            text("SELECT * FROM users WHERE id = :id"),
            {
                "id": current_user['id'],
            }
        )
        mariadb_user_data = result.fetchone()
        couchdb_user_data = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Users')].get(current_user['id'])


        if not mariadb_user_data:
            raise HTTPException(
                status_code = status.HTTP_404_NOT_FOUND,
                detail      = "User does not exist... wait hang on... you don't exist??",
            )

        return UserDetailsResponseModel(
            id              = mariadb_user_data.id,
            email_address   = mariadb_user_data.email_address,
            full_name       = couchdb_user_data.get('full_name'),
            is_admin        = mariadb_user_data.is_admin,
        )

    except Exception as e:
        mariadb.rollback()
        raise e