
import logging

from fastapi import HTTPException, Depends, status, APIRouter, Path

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.document_modification.case_document_tools import CaseDocumentTools
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import (
    get_current_user
)
from common.uuid_handling import UUIDHandling
from models.cases.add_person_request_model import AddPersonRequestModel
from models.cases.case_response_model import CaseResponseModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}", tags=["Cases"])


@router.post(
    path="/clients",
    response_model=CaseResponseModel,
    status_code=status.HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
async def add_client_to_case(
        person_to_add: AddPersonRequestModel,
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

        case_doc = doc_tools.get_document(
            case_id=case_id,
        )

        if "clients" not in case_doc:
            case_doc["clients"] = []

        if not current_user.is_admin:
            if current_user.id not in case_doc['workers']:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail="You have not got permissions to do this action"
                )

        if person_to_add.user_id in case_doc["clients"]:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Client is already added to this case"
            )

        doc_tools.add_client(
            case_id=case_id,
            user_id=person_to_add.user_id,
        )

        return doc_tools.build_response(
            doc=doc_tools.get_document(
                case_id=case_id
            )
        )

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to add client: {str(e)}"
        )


@router.delete(
    path="/clients/{client_id:path}",
    response_model=SuccessResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
async def remove_client_from_case(
        case_id: str = Path(..., description="Case ID"),
        client_id: str = Path(..., description="Client user ID to remove"),
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

        case_doc = doc_tools.get_document(
            case_id=case_id,
        )

        if not current_user.is_admin:
            if current_user.id not in case_doc['workers']:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail="You have not got permissions to do this action"
                )

        doc_tools.remove_client(
            case_id=case_id,
            user_id=client_id,
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
            detail=f"Failed to remove client: {str(e)}"
        )


@router.post(
    path="/workers",
    response_model=CaseResponseModel,
    status_code=status.HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
async def add_worker_to_case(
        person_to_add: AddPersonRequestModel,
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

        case_doc = doc_tools.get_document(
            case_id=case_id,
        )

        if "workers" not in case_doc:
            case_doc["workers"] = []

        if not current_user.is_admin:
            if current_user.id not in case_doc['workers']:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail="You have not got permissions to do this action"
                )

        if person_to_add.user_id in case_doc["workers"]:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Client is already added to this case"
            )

        doc_tools.add_case_worker(
            case_id=case_id,
            user_id=person_to_add.user_id,
        )

        return doc_tools.build_response(
            doc=doc_tools.get_document(
                case_id=case_id
            )
        )

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to add client: {str(e)}"
        )


@router.post(
    path="/workers/{worker_id:path}",
    response_model=SuccessResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
async def remove_worker_from_case(
        case_id: str = Path(..., description="Case ID"),
        worker_id: str = Path(..., description="Worker user ID to remove"),
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

        case_doc = doc_tools.get_document(
            case_id=case_id,
        )

        if not current_user.is_admin:
            if current_user.id not in case_doc['workers']:
                raise HTTPException(
                    status_code=status.HTTP_403_FORBIDDEN,
                    detail="You have not got permissions to do this action"
                )

        doc_tools.remove_case_worker(
            case_id=case_id,
            user_id=worker_id,
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
            detail=f"Failed to remove client: {str(e)}"
        )
