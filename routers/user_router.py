import logging
import mimetypes

from fastapi import HTTPException, Depends, status, APIRouter, Path, Query, UploadFile, File, Form
from sqlalchemy import text

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.utilities.configuration_utilities import get_configuration
from common.document_modification.file_document_tools import create_file
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.parameters import pagination_params, sort_params, user_filter_params
from common.utilities.security_utilities import get_current_user
from common.document_modification.user_document_tools import UserDocumentTools
from common.uuid_handling import UUIDHandling
from enumerators.property_type import PropertyType
from models.success_response_model import SuccessResponseModel
from models.user.paginated_users_response_model import PaginatedUsersResponse
from models.user.user_details_response_model import UserDetailsResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/users", tags=["Users"])



@router.get(
    path="",
    response_model=PaginatedUsersResponse,
    status_code=status.HTTP_200_OK,
    tags=[
        "Users",
    ]
)
async def search_users(
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
        doc_tools = UserDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        if current_user.is_admin:
            selector = {}
        else:
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"wip lol"
            )

        return await doc_tools.get_users_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
        )

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.get(
    path="/{user_id:path}",
    response_model=UserDetailsResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Users",
    ]
)
async def get_user_by_id(
        user_id: str = Path(..., description="User ID to fetch"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not UUIDHandling.is_valid(user_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc_tools = UserDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        user_doc = doc_tools.build_response(
            doc=doc_tools.get_document(
                user_id=user_id,
            )
        )

        mariadb_user_data = mariadb.execute(
            text("""
                SELECT * FROM users WHERE id=:user_id;
            """),
            {
                "user_id": user_id,
            }
        ).fetchone()
        user_doc.is_admin = mariadb_user_data.is_admin
        user_doc.email_address = mariadb_user_data.email_address

        return user_doc

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch case: {str(e)}"
        )
