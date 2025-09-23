import logging
import mimetypes
from datetime import datetime
from typing import Optional

from fastapi import HTTPException, Depends, status, APIRouter, Path, Query, UploadFile, File, Form
from fastapi.responses import JSONResponse

from common.databases.couchdb_interactions import get_couchdb_connection
from common.databases.mariadb_interactions import get_mariadb_connection
from common.utilities.configuration import get_configuration
from common.utilities.security_utilities import (
    get_current_user
)
from common.utilities.user_utilities import get_user_details, check_user_access, get_user_properties, \
    save_user_property, find_shared_cases
from enumerators.property_type import PropertyType
from models.debug.ping_response_model import PingResponseModel
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel
from models.user.user_details_response_model import UserDetailsResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/server", tags=["Users"])

@router.get("/ping", response_model=SimplifiedUserDetailsResponseModel)
async def get_current_user_details(
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        return PingResponseModel(
            response="pong!"
        )

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error fetching current user details: {e}")
        mariadb.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to fetch user details"
        )
