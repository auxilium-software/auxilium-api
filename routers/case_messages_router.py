import logging
from datetime import datetime

from fastapi import HTTPException, Depends, APIRouter, Path, Body
from fastapi import status as http_status
from starlette.status import HTTP_201_CREATED

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.utilities.case_utilities import get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType
from models.cases.message_creation_request_model import MessageCreationRequestModel
from models.message.message_response_model import MessageResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}/messages", tags=["Cases"])


@router.post(
    path="",
    response_model=MessageResponseModel,
    status_code=HTTP_201_CREATED,
    tags=[
        "Cases",
    ]
)
async def create_message_for_case(
        case_id: str = Path(..., description="Case ID"),
        request: MessageCreationRequestModel = Body(...),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        case_doc = get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id)

        message_id = UUIDHandling.v5s(DatabaseObjectType.MESSAGE)

        if 'messages' not in case_doc:
            case_doc['messages'] = {}

        message_doc = {
            '_id': message_id,
            'created_at': datetime.utcnow().isoformat(),
            'subject': request.subject,
            'content': request.content,
            'sender_id': current_user.id,
            'is_urgent': request.is_urgent if hasattr(request, 'is_urgent') else False,
            'read_by': {},
            'updated_at': None,
        }

        case_doc['messages'].append(f"auxmsg://%%couchdb%%/{message_id}")
        case_doc['updated_at'] = datetime.utcnow().isoformat()

        cases_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]
        messages_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Messages')]

        cases_db.save(case_doc)
        messages_db.save(message_doc)

        return MessageResponseModel(
            id=message_id,
            created_at=datetime.utcnow(),
            case_id=case_id,
            subject=message_doc["subject"],
            content=message_doc["content"],
            sender_id=current_user.id,
            is_urgent=message_doc['is_urgent'],
            read_by={},
        )

    except HTTPException as e:
        PRIMARY_LOGGER.exception(e)
        raise
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating message for case: {str(e)}"
        )

