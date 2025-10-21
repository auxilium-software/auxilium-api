import logging
import mimetypes
from datetime import datetime
from typing import Optional

from fastapi import HTTPException, Depends, status, APIRouter, Path, Query, UploadFile, File, Form
from fastapi.responses import JSONResponse
from starlette.responses import FileResponse

from common.databases.couchdb_interactions import get_couchdb_connection, get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_connection, get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.configuration import get_configuration
from common.utilities.file_utilities import get_file_details, get_file_contents
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.parameters import pagination_params, sort_params, case_filter_params, user_filter_params
from common.utilities.security_utilities import get_current_user
from common.utilities.user_utilities import get_user_details, check_user_access, get_user_properties, save_user_property, find_shared_cases, get_users_with_filter
from enumerators.property_type import PropertyType
from models.file.file_details_response_model import FileDetailsResponseModel
from models.user.paginated_users_response_model import PaginatedUsersResponse
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel
from models.user.user_details_response_model import UserDetailsResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/files", tags=["Files"])



@router.get("/{file_id:path}", response_model=FileDetailsResponseModel)
async def search_files(
        file_id: str = Path(..., description="File ID"),
        pagination=Depends(pagination_params),
        filters=Depends(user_filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if current_user.is_admin:
            selector = {}
        else:
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"wip lol"
            )

        mariadb_data, couchdb_data = get_file_details(
            file_id, mariadb, couchdb, configuration
        )

        return FileDetailsResponseModel(
            id=couchdb_data.get('_id'),
            filename=couchdb_data.get('filename'),
            content_type=couchdb_data.get('content_type'),
            hash=couchdb_data.get('hash'),
            size=couchdb_data.get('size'),
            uploaded_at=couchdb_data.get('uploaded_at'),
            uploaded_by=couchdb_data.get('uploaded_by'),
            contents=get_file_contents(couchdb_data.get('_id'), configuration),
        )
    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )
