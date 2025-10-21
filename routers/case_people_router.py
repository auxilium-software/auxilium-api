
import logging
from typing import Optional, List, Dict, Any

from fastapi import HTTPException, Depends, status, APIRouter, Query, Path
from pydantic import BaseModel

from common.databases.couchdb_interactions import get_couchdb_connection, get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_connection, get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.case_utilities import get_cases_with_filter, get_cases_collection, build_case_response, \
    get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.parameters import pagination_params, case_filter_params, sort_params
from common.utilities.security_utilities import (
    get_current_user
)
from models.cases.add_person_request_model import AddPersonRequestModel
from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_cases_response_model import PaginatedCasesResponse
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}", tags=["Cases"])


@router.post("/clients", response_model=CaseResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        if "clients" not in doc:
            doc["clients"] = []

        if person_to_add.user_id in doc["clients"]:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Client is already added to this case"
            )

        doc["clients"].append(person_to_add.user_id)

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(doc)

        return build_case_response(doc)

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to add client: {str(e)}"
        )


@router.delete("/clients/{client_id:path}", response_model=SuccessResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        if "clients" not in doc or client_id not in doc["clients"]:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Client not found in this case"
            )

        doc["clients"].remove(client_id)

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(doc)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to remove client: {str(e)}"
        )


@router.post("/workers", response_model=CaseResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        if "workers" not in doc:
            doc["workers"] = []

        if person_to_add.user_id in doc["workers"]:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="Worker is already assigned to this case"
            )

        doc["workers"].append(person_to_add.user_id)

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(doc)

        return build_case_response(doc)

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to add worker: {str(e)}"
        )


@router.delete("/workers/{worker_id:path}", response_model=SuccessResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        if "workers" not in doc or worker_id not in doc["workers"]:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Worker not found in this case"
            )

        doc["workers"].remove(worker_id)

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(doc)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to remove worker: {str(e)}"
        )
