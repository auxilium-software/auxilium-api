import logging

from fastapi import HTTPException, Depends, status, APIRouter
from sqlalchemy import text
from datetime import datetime, timedelta
from typing import Optional, List
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
from models.cases.case_response_model import CaseResponseModel
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

router = APIRouter(prefix="/api/v3/cases", tags=["Cases"])


@router.get(
    path="/mine",
    response_model=List[CaseResponseModel],
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases"
    ],
)
async def mine(
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        query = {
            "selector": {
                "clients": {
                    "$elemMatch": {
                        "$eq": current_user['id'],
                    }
                }
            }
        }
        results = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].find(query)

        builder = []
        for doc in results:
            builder.append(CaseResponseModel(
                id=doc['_id'],
                sensitivity=doc['sensitivity'],
                title=doc['title'],
                status=doc['status'],
                brief_description=doc['brief_description'],
                case_referrer=doc['case_referrer'],
                description=doc['description'],
                workers=doc['workers'],
                clients=doc['clients'],
                additional_properties=doc['additional_properties'],
            ))

        return builder

    except Exception as e:
        raise e


@router.get(
    path="/assigned",
    response_model=List[CaseResponseModel],
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases"
    ],
)
async def assigned(
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        query = {
            "selector": {
                "workers": {
                    "$elemMatch": {
                        "$eq": current_user['id'],
                    }
                }
            }
        }
        results = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].find(query)

        builder = []
        for doc in results:
            builder.append(CaseResponseModel(
                id=doc['_id'],
                sensitivity=doc['sensitivity'],
                title=doc['title'],
                status=doc['status'],
                brief_description=doc['brief_description'],
                case_referrer=doc['case_referrer'],
                description=doc['description'],
                workers=doc['workers'],
                clients=doc['clients'],
                additional_properties=doc['additional_properties'],
            ))

        return builder

    except Exception as e:
        raise e