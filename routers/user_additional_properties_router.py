import logging
from datetime import datetime
from typing import Optional, Any

from fastapi import HTTPException, Depends, status, APIRouter, Path, Body
from starlette.status import HTTP_201_CREATED, HTTP_200_OK

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.property_name_handler import PropertyNameHandler
from common.utilities.security_utilities import get_current_user
from common.uuid_handling import UUIDHandling
from common.document_modification.user_document_tools import UserDocumentTools
from enumerators.property_type import PropertyType
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/users/{user_id:path}", tags=["Users"])


@router.post(
    path="/additional_properties/{property_name:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_201_CREATED,
    tags=["Users"]
)
async def create_user_property(
        user_id: str = Path(..., description="User ID"),
        property_name: str = Path(..., description="Property name (pretty or normalized)"),
        content: Any = Body(None),
        content_type: Optional[str] = None,
        display_name: Optional[str] = Body(None, description="Override display name"),
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
            mariadb=mariadb,
            current_user=current_user
        )

        if not doc_tools.check_user_access(user_id):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to modify this user's properties"
            )

        storage_key, auto_display_name = PropertyNameHandler.handle_property_name(property_name)
        final_display_name = display_name or auto_display_name

        if isinstance(content, dict) and 'content' in content:
            actual_content = content['content']
            content_type = content.get('content_type', PropertyType.TEXT)
        else:
            actual_content = content if content is not None else ""
            content_type = content_type or PropertyType.TEXT

        user_doc = doc_tools.get_document(
            user_id=user_id,
        )

        if storage_key in user_doc['additional_properties']:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail=f"Property '{final_display_name}' already exists. Use PATCH to update."
            )

        property_data = PropertyNameHandler.create_property_metadata(
            final_display_name,
            actual_content,
            content_type,
            current_user.id
        )

        doc_tools.save_additional_property(user_id, storage_key, property_data)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to create property"
        )


@router.patch(
    path="/additional_properties/{property_name:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_200_OK,
    tags=["Users"]
)
async def update_user_property(
        user_id: str = Path(..., description="User ID"),
        property_name: str = Path(..., description="Property name"),
        content: Any = Body(None),
        content_type: Optional[str] = None,
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
            mariadb=mariadb,
            current_user=current_user
        )

        if not doc_tools.check_user_access(user_id):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to modify this user's properties"
            )

        if isinstance(content, dict) and 'content' in content:
            actual_content = content['content']
            content_type = content.get('content_type', PropertyType.TEXT)
        else:
            actual_content = content if content is not None else ""
            content_type = content_type or PropertyType.TEXT

        user_doc = doc_tools.get_document(
            user_id=user_id,
        )

        if property_name not in user_doc['additional_properties']:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail=f"Property '{property_name}' not found"
            )

        property_data = user_doc['additional_properties'][property_name]
        if isinstance(property_data, dict):
            property_data['content'] = actual_content
            property_data['content_type'] = content_type
            property_data['updated_at'] = datetime.utcnow().isoformat()
            property_data['updated_by'] = current_user.id
        else:
            # Handle legacy properties that might not be dicts
            property_data = {
                'content': actual_content,
                'content_type': content_type,
                'updated_at': datetime.utcnow().isoformat(),
                'updated_by': current_user.id
            }

        # Save the updated property
        doc_tools.save_additional_property(user_id, property_name, property_data)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to update property"
        )


@router.delete(
    path="/additional_properties/{property_name:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_200_OK,
    tags=["Users"]
)
async def delete_user_property(
        user_id: str = Path(..., description="User ID"),
        property_name: str = Path(..., description="Property name"),
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
            mariadb=mariadb,
            current_user=current_user
        )

        if not doc_tools.check_user_access(user_id):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to modify this user's properties"
            )

        doc_tools.delete_additional_property(user_id, property_name)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to delete property"
        )
