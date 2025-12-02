import logging
import mimetypes

from fastapi import HTTPException, Depends, status, APIRouter, Path, UploadFile, Form, File

from common.couchdb_document_structures.case_document import CaseDocument
from common.couchdb_document_structures.enumerators.case_sensitivity_enum import CaseSensitivityEnum
from common.couchdb_document_structures.enumerators.case_status_enum import CaseStatusEnum
from common.databases.couchdb_interactions import get_couchdb_dependency
from common.utilities.case_utilities import get_cases_with_filter, build_case_response, \
    get_single_case_and_handle_permissions
from common.utilities.configuration_utilities import get_configuration
from common.utilities.file_utilities import create_file
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.parameters import pagination_params, case_filter_params, sort_params
from common.utilities.security_utilities import (
    get_current_user
)
from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType
from enumerators.property_type import PropertyType
from models.cases.case_creation_request_model import CaseCreationRequestModel
from models.cases.case_response_model import CaseResponseModel
from models.cases.case_update_request_model import CaseUpdateRequestModel
from models.cases.paginated_cases_response_model import PaginatedCasesResponse
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases", tags=["Cases"])


@router.get(
    path="/mine",
    response_model=PaginatedCasesResponse,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
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
    path="/assigned",
    response_model=PaginatedCasesResponse,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
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


        case_doc_builder = CaseDocument()
        case_doc_builder.set_required_properties(
            _id = case_id,
            created_by=current_user.id,

            title=request.title,
            description=request.description,
            sensitivity=CaseSensitivityEnum.CONFIDENTIAL,
            status=CaseStatusEnum.OPEN
        )
        case_doc_builder.clients = [
            current_user.id
        ]

        other = {
            # "on_behalf_of": request.on_behalf_of,
            # "data_processing_consent": request.data_processing_consent,
            # "how_did_you_find_out_about_our_service": request.how_did_you_find_out_about_our_service
        }

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(case_doc_builder.to_json())
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
            messages                = case_doc.get('messages'),
            files                   = case_doc.get('files'),
        )


    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise e


@router.get(
    path="",
    response_model=PaginatedCasesResponse,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
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
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.post(
    path="/{case_id:path}/upload",
    response_model=SuccessResponseModel,
    status_code=status.HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
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
        if not UUIDHandling.is_valid(case_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        _ = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

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
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to upload file"
        )


@router.get(
    path="/{case_id:path}",
    response_model=CaseResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
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
        if not UUIDHandling.is_valid(case_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)
        return build_case_response(doc)

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


@router.patch(
    path="/{case_id:path}",
    response_model=CaseResponseModel,
    status_code=status.HTTP_200_OK,
    tags=["Cases"],
)
async def update_case(
        request: CaseUpdateRequestModel,
        case_id: str = Path(..., description="Case ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        couchdb=Depends(get_couchdb_dependency),
):
    try:
        if not UUIDHandling.is_valid(case_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc = get_single_case_and_handle_permissions(
            configuration, couchdb, current_user, case_id
        )

        update_data = request.model_dump(exclude_unset=True)
        if not update_data:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="At least one field must be provided for update"
            )

        for field, value in update_data.items():
            doc[field] = value

        db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]
        db.save(doc)

        updated_doc = db.get(case_id)
        return build_case_response(updated_doc)

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to update case: {str(e)}"
        )
