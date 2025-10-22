import logging
import mimetypes
from typing import Optional, List, Dict, Any

from fastapi import HTTPException, Depends, status, APIRouter, Query, Path, UploadFile, Form, File
from pydantic import BaseModel

from common.databases.couchdb_interactions import get_couchdb_connection, get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_connection, get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.case_utilities import get_cases_with_filter, get_cases_collection, build_case_response, \
    get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.file_utilities import create_file
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.parameters import pagination_params, case_filter_params, sort_params
from common.utilities.security_utilities import (
    get_current_user
)
from common.uuid_handling import UUIDHandling
from enumerators.case_status import CaseStatus
from enumerators.database_object_type import DatabaseObjectType
from enumerators.property_type import PropertyType
from models.cases.add_person_request_model import AddPersonRequestModel
from models.cases.case_creation_request_model import CaseCreationRequestModel
from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_cases_response_model import PaginatedCasesResponse
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases", tags=["Cases"])


@router.get("/mine", response_model=PaginatedCasesResponse)
async def get_my_cases(
        pagination=Depends(pagination_params),
        filters=Depends(case_filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        selector = {
            "clients": {
                "$elemMatch": {
                    "$eq": current_user.id
                }
            }
        }

        return await get_cases_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
        )
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.get("/assigned", response_model=PaginatedCasesResponse)
async def get_assigned_cases(
        pagination=Depends(pagination_params),
        filters=Depends(case_filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        selector = {
            "workers": {
                "$elemMatch": {
                    "$eq": current_user.id
                }
            }
        }

        return await get_cases_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
        )
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.post(
    path="",
    response_model=CaseResponseModel,
    status_code=status.HTTP_201_CREATED,
    tags=[
        "Cases"
    ],
)
async def create_case(
        request: CaseCreationRequestModel,
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
        client_ip: str = None,
):
    try:
        case_id = UUIDHandling().v5s(object_type=DatabaseObjectType.CASE)

        case_doc = {
            "_id": case_id,
            "title": request.title,
            "description": request.description,
            "sensitivity": None,
            "status": CaseStatus.NEW_CASE.value,
            "case_referrer": request.case_referrer,
            "additional_properties": {},
            "workers": [],
            "clients": [
                current_user.id
            ],
            "todos": {},
            "timeline": {},
        }
        other = {
            # "on_behalf_of": request.on_behalf_of,
            # "data_processing_consent": request.data_processing_consent,
            # "how_did_you_find_out_about_our_service": request.how_did_you_find_out_about_our_service
        }

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(case_doc)
        case_doc = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].get(case_id)

        return CaseResponseModel(
            id                      = case_doc.get('_id'),
            sensitivity             = case_doc.get('sensitivity'),
            status                  = case_doc.get('status'),
            case_referrer           = case_doc.get('case_referrer'),
            title                   = case_doc.get('title'),
            description             = case_doc.get('description'),
            additional_properties   = case_doc.get('additional_properties'),
            workers                 = case_doc.get('workers'),
            clients                 = case_doc.get('clients'),
            todos                   = case_doc.get('todos'),
            timeline                = case_doc.get('timeline'),
        )

    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise e


@router.get("", response_model=PaginatedCasesResponse)
async def search_cases(
        pagination=Depends(pagination_params),
        filters=Depends(case_filter_params),
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
            selector = {
                "$or": [
                    {
                        "clients": {
                            "$elemMatch": {
                                "$eq": current_user.id
                            }
                        }
                    },
                    {
                        "workers": {
                            "$elemMatch": {
                                "$eq": current_user.id
                            }
                        }
                    }
                ]
            }

        return await get_cases_with_filter(
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


@router.post("/{case_id:path}/upload", response_model=SuccessResponseModel)
async def upload_file(
        file: UploadFile = File(...),
        description: str = Form(...),
        case_id: str = Path(..., description="Case ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        content = await file.read()
        content_type = file.content_type or mimetypes.guess_type(file.filename)[0] or PropertyType.BINARY

        create_file(
            document_type="Cases",
            document_id=case_id,
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


@router.get("/{case_id:path}", response_model=CaseResponseModel)
async def get_single_case(
        case_id: str = Path(..., description="Case ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)
        return build_case_response(doc)

    except HTTPException as e:
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch case: {str(e)}"
        )

