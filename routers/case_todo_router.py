import logging
import uuid
from datetime import datetime
from typing import Optional

from fastapi import HTTPException, Depends, APIRouter, Path, Body
from fastapi import status as http_status
from starlette.status import HTTP_201_CREATED, HTTP_200_OK

from common.couchdb_document_structures.sub_structures.case_todo_object import CaseTodoObject
from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency, publish_message
from common.document_modification.case_document_tools import CaseDocumentTools
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from common.utilities.timeline_utilities import build_timeline_object
from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType
from enumerators.timeline_entry_type import TimelineEntryType
from enumerators.todo_status import TodoStatus
from models.cases.todo_creation_request_model import TodoCreationRequestModel
from models.cases.todo_response_model import TodoResponseModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}/todos", tags=["Cases"])


@router.post(
    path="",
    response_model=TodoResponseModel,
    status_code=HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
async def add_single_todo_to_single_case(
        case_id: str = Path(..., description="Case ID"),
        todo_request: TodoCreationRequestModel = Body(...),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not UUIDHandling.is_valid(case_id):
            raise HTTPException(
                status_code=http_status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        case_doc = doc_tools.get_document(
            case_id=case_id
        )

        todo_id = UUIDHandling.v5s(
            object_type=DatabaseObjectType.CASE_TODO_ITEM
        )

        todo_entry = CaseTodoObject()
        todo_entry.set_required_properties(
            created_at      = datetime.utcnow(),
            created_by      = current_user.id,

            summary         = todo_request.summary,
            description     = todo_request.description,
            status          = TodoStatus.NEEDS_ACTION,
            priority        = todo_request.priority,

            due_date        = todo_request.due_date,
            completed_at    = None,
            assigned_to     = todo_request.assigned_to,
        )

        if todo_request.reminder:
            todo_entry.set_reminder()  # todo_request.reminder.isoformat())



        if todo_request.assigned_to:  # and todo_request.assigned_to != current_user.id:
            publish_message(
                connection=rabbitmq,
                queue_key="Notifications",
                message={
                    "case_id": case_id,
                    "todo_id": todo_id,
                },
            )

        todo_doc = doc_tools.add_todo(
            case_id=case_id,
            todo_object=todo_entry
        )

        return TodoResponseModel(
            id=todo_id,
            case_id=case_id,
            summary=todo_request.summary,
            description=todo_request.description,
            status=TodoStatus.NEEDS_ACTION,
            priority=todo_request.priority,
            created_at=datetime.utcnow(),
            created_by=current_user.id,
            due_date=todo_request.due_date,
            completed_at=None,
            assigned_to=todo_request.assigned_to,
        )

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating todo item for case: {str(e)}"
        )


@router.patch(
    path="/{todo_id:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
async def update_todo_status(
        case_id: str = Path(..., description="Case ID"),
        todo_id: str = Path(..., description="Todo ID"),
        status: TodoStatus = Body(..., description="New status"),
        notes: Optional[str] = Body(None, description="Completion notes"),
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
                status_code=http_status.HTTP_400_BAD_REQUEST,
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
        todo_object = doc_tools.get_todo(
            case_id=case_id,
            todo_id=todo_id,
        )

        doc_tools.update_todo_properties(
            case_id=case_id,
            todo_id=todo_id,
            properties={
                'status': status,
            }
        )

        if status == TodoStatus.COMPLETED:
            doc_tools.update_todo_properties(
                case_id=case_id,
                todo_id=todo_id,
                properties={
                    'completed_at': datetime.utcnow().isoformat(),
                    'completed_by': current_user.id,
                }
            )

            if notes:
                doc_tools.update_todo_properties(
                    case_id=case_id,
                    todo_id=todo_id,
                    properties={
                        'completion_notes': notes,
                    }
                )

        return SuccessResponseModel()

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error updating todo: {str(e)}"
        )


@router.delete(
    path="/{todo_id:path}",
    response_model=SuccessResponseModel,
    status_code=HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
async def delete_todo(
        case_id: str = Path(..., description="Case ID"),
        todo_id: str = Path(..., description="Todo ID"),
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
                status_code=http_status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        doc_tools.delete_todo(
            case_id=case_id,
            todo_id=todo_id,
        )

        return SuccessResponseModel()

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error deleting todo: {str(e)}"
        )
