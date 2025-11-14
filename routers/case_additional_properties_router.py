import json
import logging
from datetime import datetime
from typing import Optional, Any

from fastapi import HTTPException, Depends, status, APIRouter, Path, Body
from fastapi.responses import Response, JSONResponse
from starlette.status import HTTP_201_CREATED, HTTP_200_OK

from common.databases.couchdb_interactions import get_couchdb_connection, get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.property_name_handler import PropertyNameHandler
from common.utilities.security_utilities import (
    get_current_user
)
from common.utilities.case_utilities import get_single_case_and_handle_permissions, get_case_properties, \
    save_case_property
from common.uuid_handling import UUIDHandling
from enumerators.property_type import PropertyType
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}", tags=["Cases"])


@router.post(
    path="/additional_properties/{property_name:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
async def create_case_property(
        case_id: str = Path(..., description="Case ID"),
        property_name: str = Path(..., description="Property name (pretty or normalized)"),
        content: Any = Body(None),
        content_type: Optional[str] = None,
        display_name: Optional[str] = Body(None, description="Override display name"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        _ = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        storage_key, auto_display_name = PropertyNameHandler.handle_property_name(property_name)
        final_display_name = display_name or auto_display_name

        if isinstance(content, dict) and 'content' in content:
            actual_content = content['content']
            content_type = content.get('content_type', PropertyType.TEXT)
        else:
            actual_content = content if content is not None else ""
            content_type = content_type or PropertyType.TEXT

        properties = get_case_properties(case_id, couchdb, configuration)

        if storage_key in properties:
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

        save_case_property(case_id, storage_key, property_data, couchdb, configuration)

        return SuccessResponseModel()

    except HTTPException as e:
        # mariadb.rollback()
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
    tags=[
        "Cases",
    ]
)
async def update_case_property(
        case_id: str = Path(..., description="Case ID"),
        property_name: str = Path(..., description="Property name"),
        content: Any = Body(None),
        content_type: Optional[str] = None,
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        _ = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        if isinstance(content, dict) and 'content' in content:
            actual_content = content['content']
            content_type = content.get('content_type', PropertyType.TEXT)
        else:
            actual_content = content if content is not None else ""
            content_type = content_type or PropertyType.TEXT

        properties = get_case_properties(case_id, couchdb, configuration)

        if property_name not in properties:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Property '{property_name}' not found"
            )

        property_data = properties[property_name]
        if isinstance(property_data, dict):
            property_data['content'] = actual_content
            property_data['content_type'] = content_type
            property_data['updated_at'] = datetime.utcnow().isoformat()
            property_data['updated_by'] = current_user.id
        else:
            property_data = {
                'content': actual_content,
                'content_type': content_type,
                'updated_at': datetime.utcnow().isoformat(),
                'updated_by': current_user.id
            }

        save_case_property(case_id, property_name, property_data, couchdb, configuration)

        return SuccessResponseModel()

    except HTTPException as e:
        # mariadb.rollback()
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
    tags=[
        "Cases",
    ]
)
async def delete_case_property(
        case_id: str = Path(..., description="Case ID"),
        property_name: str = Path(..., description="Property name"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        _ = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        cases_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]
        case_doc = cases_db.get(case_id)

        if not case_doc or property_name not in case_doc['additional_properties']:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Property '{property_name}' not found"
            )

        del case_doc['additional_properties'][property_name]
        case_doc['updated_at'] = datetime.utcnow().isoformat()

        cases_db.save(case_doc)

        return SuccessResponseModel()

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to delete property"
        )
