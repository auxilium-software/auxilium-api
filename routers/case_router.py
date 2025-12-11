import logging
import mimetypes

from fastapi import HTTPException, Depends, status, APIRouter, Path, UploadFile, Form, File

from common.couchdb_document_structures.case_document import CaseDocument
from common.couchdb_document_structures.enumerators.case_sensitivity_enum import CaseSensitivityEnum
from common.couchdb_document_structures.enumerators.case_status_enum import CaseStatusEnum
from common.databases.couchdb_interactions import get_couchdb_dependency
from common.document_modification.case_document_tools import CaseDocumentTools
from common.utilities.configuration_utilities import get_configuration
from common.document_modification.file_document_tools import create_file
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
        case_doc_builder = CaseDocument()
        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        case_id = UUIDHandling().v5s(object_type=DatabaseObjectType.CASE)

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

        doc_tools.save_document(
            document_builder=case_doc_builder
        )

        return doc_tools.get_document(case_id=case_id)


    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise e


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
        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        selector = {
            "clients": {
                "$elemMatch": {
                    "$eq": current_user.id
                }
            }
        }

        return await doc_tools.get_cases_with_filter(
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
        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        selector = {
            "workers": {
                "$elemMatch": {
                    "$eq": current_user.id
                }
            }
        }
        return await doc_tools.get_cases_with_filter(
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
        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

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

        return await doc_tools.get_cases_with_filter(
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

        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        _ = doc_tools.get_document(
            case_id=case_id
        )

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

        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        doc = doc_tools.get_document(
            case_id=case_id
        )

        return doc_tools.build_response(
            doc=doc
        )

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

        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        update_data = request.model_dump(exclude_unset=True)
        if not update_data:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="At least one field must be provided for update"
            )


        doc_tools.save_multiple_properties(
            case_id=case_id,
            properties=update_data,
        )

        updated_doc = doc_tools.get_document(
            case_id=case_id
        )

        return doc_tools.build_response(
            doc=updated_doc
        )

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to update case: {str(e)}"
        )
