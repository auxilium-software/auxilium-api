import logging
import uuid
from datetime import datetime
from typing import Optional

from fastapi import HTTPException, Depends, APIRouter, Path, Body
from fastapi import status as http_status
from starlette.status import HTTP_201_CREATED

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency, publish_message
from common.databases.redis_interactions import get_redis_dependency
from common.utilities.case_utilities import get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from enumerators.todo_status import TodoStatus
from models.cases.todo_creation_request_model import TodoCreationRequestModel
from models.cases.todo_response_model import TodoResponseModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}/todos", tags=["Cases"])


@router.post("", response_model=TodoResponseModel, status_code=HTTP_201_CREATED)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        todo_id = str(uuid.uuid4())

        if 'todos' not in doc:
            doc['todos'] = []

        todo_entry = {
            'summary': todo_request.summary,
            'description': todo_request.description,
            'status': TodoStatus.NEEDS_ACTION,
            'priority': todo_request.priority,
            'created_at': datetime.utcnow().isoformat(),
            'created_by': current_user.id,
            'due_date': todo_request.due_date.isoformat(),
            'completed_at': None,
            'assigned_to': todo_request.assigned_to,
            'reminders': []
        }

        if todo_request.reminder:
            todo_entry['reminders'].append(todo_request.reminder.isoformat())

        doc['todos'][todo_id] = todo_entry
        doc['updated_at'] = datetime.utcnow().isoformat()

        cases_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]
        cases_db.save(doc)

        if todo_request.assigned_to:
            publish_message(
                connection=rabbitmq,
                queue_key="Notifications",
                message={
                    "case_id": case_id,
                    "todo_id": todo_id,
                },
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
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating todo item for case: {str(e)}"
        )


@router.patch("/{todo_id:path}", response_model=SuccessResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        todos = doc.get('todos', [])
        todo_index = next((i for i, t in enumerate(todos) if t['id'] == todo_id), None)

        if todo_index is None:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Todo not found"
            )

        todo = todos[todo_index]
        todo['status'] = status
        todo['updated_at'] = datetime.utcnow().isoformat()
        todo['updated_by'] = current_user.id

        if status == TodoStatus.COMPLETED:
            todo['completed_at'] = datetime.utcnow().isoformat()
            todo['completed_by'] = current_user.id
            if notes:
                todo['completion_notes'] = notes

        doc['updated_at'] = datetime.utcnow().isoformat()

        # Save
        cases_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]
        cases_db.save(doc)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error updating todo: {str(e)}"
        )


@router.delete("/{todo_id:path}", response_model=SuccessResponseModel)
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
        doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        doc['timeline'][str(uuid.uuid4())].append({
            "type": "TODO",
            "original_data": doc.get('todos', todo_id)
        })

        todos = doc.get('todos', [])
        original_length = len(todos)

        del todos[todo_id]

        if len(doc['todos']) == original_length:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Todo not found"
            )

        doc['updated_at'] = datetime.utcnow().isoformat()

        couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')].save(doc)

        return SuccessResponseModel()

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error deleting todo: {str(e)}"
        )
