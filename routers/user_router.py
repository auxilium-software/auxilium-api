import logging
import mimetypes
from datetime import datetime
from typing import Optional

from fastapi import HTTPException, Depends, status, APIRouter, Path, Query, UploadFile, File, Form
from fastapi.responses import JSONResponse

from common.databases.couchdb_interactions import get_couchdb_connection, get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_connection, get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.configuration import get_configuration
from common.utilities.file_utilities import create_file
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.parameters import pagination_params, sort_params, case_filter_params, user_filter_params
from common.utilities.security_utilities import get_current_user
from common.utilities.user_utilities import get_user_details, check_user_access, get_user_properties, save_user_property, find_shared_cases, get_users_with_filter
from enumerators.property_type import PropertyType
from models.success_response_model import SuccessResponseModel
from models.user.paginated_users_response_model import PaginatedUsersResponse
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel
from models.user.user_details_response_model import UserDetailsResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/users", tags=["Users"])



@router.get("", response_model=PaginatedUsersResponse)
async def search_users(
        pagination=Depends(pagination_params),
        filters=Depends(user_filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
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

        return await get_users_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
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


@router.get("/{user_id:path}", response_model=UserDetailsResponseModel)
async def get_user_by_id(
        user_id: str = Path(..., description="User ID to fetch"),
        include_properties: bool = Query(True, description="Include additional properties"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if user_id == 'me':
            user_id = current_user.id

        if not check_user_access(current_user, user_id, couchdb, configuration):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to view this user"
            )

        mariadb_data, couchdb_data = get_user_details(
            user_id, mariadb, couchdb, configuration
        )

        if not mariadb_data:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="User not found"
            )

        additional_properties = {}
        if include_properties:
            additional_properties = get_user_properties(user_id, couchdb, configuration)

        return UserDetailsResponseModel(
            id=mariadb_data.id,
            email_address=mariadb_data.email_address,
            full_name=couchdb_data.get('full_name') if couchdb_data else None,
            is_admin=mariadb_data.is_admin,
            additional_properties=additional_properties,
            files=couchdb_data.get('files') if couchdb_data else [],
            created_at=mariadb_data.created_at,
            last_updated_at=couchdb_data.get('last_updated_at') if couchdb_data else None,
        )

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        mariadb.rollback()
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to fetch user details"
        )


@router.post("/{user_id:path}/upload", response_model=SuccessResponseModel)
async def upload_file(
        file: UploadFile = File(...),
        description: str = Form(...),
        user_id: str = Path(..., description="User ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if user_id == 'me':
            user_id = current_user.id

        if not check_user_access(current_user, user_id, couchdb, configuration):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to upload files for this user"
            )

        content = await file.read()
        content_type = file.content_type or mimetypes.guess_type(file.filename)[0] or PropertyType.BINARY

        create_file(
            document_type="Users",
            document_id=user_id,
            file_name=file.filename,
            file_type=content_type,
            uploaded_by=current_user.id,
            file_contents=content.hex() if isinstance(content, bytes) else content,
            description=description,
            couchdb=couchdb,
            config=configuration,
        )

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to upload file"
        )


