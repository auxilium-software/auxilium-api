import logging
from datetime import datetime

from fastapi import HTTPException, Depends, APIRouter, Path, Body
from fastapi import status as http_status
from starlette.status import HTTP_201_CREATED

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency
from common.document_modification.case_document_tools import CaseDocumentTools
from common.document_modification.message_document_tools import get_message_details
from common.utilities.configuration_utilities import get_configuration
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
        doc_tools = CaseDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            current_user=current_user,
        )

        case_doc = doc_tools.get_document(
            case_id=case_id,
        )

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

        case_doc['messages'].append(f"auxmsg://%%default%%/{message_id}")
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
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating message for case: {str(e)}"
        )



@router.get(
    path="/{message_id:path}",
    response_model=MessageResponseModel,
    status_code=http_status.HTTP_200_OK,
    tags=[
        "Cases",
    ]
)
async def get_single_message_from_case(
        message_id: str = Path(..., description="Message ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not UUIDHandling.is_valid(message_id):
            raise HTTPException(
                status_code=http_status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        if current_user.is_admin:
            selector = {}
        else:
            raise HTTPException(
                status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"wip lol"
            )

        _, couchdb_data = get_message_details(
            message_id, mariadb, couchdb, configuration
        )

        if current_user.id not in couchdb_data.get('read_by'):
            couchdb_data['read_by'][current_user.id] = datetime.utcnow().isoformat()
            couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', 'Messages')].save(couchdb_data)

        return MessageResponseModel(
            id=message_id,
            subject=couchdb_data.get('subject'),
            content=couchdb_data.get('content'),
            sender_id=couchdb_data.get('sender_id'),
            sender_name=couchdb_data.get('sender_name'),
            is_urgent=couchdb_data.get('is_urgent', False),
            is_read=couchdb_data.get('is_read', False),
            created_at=datetime.fromisoformat(couchdb_data.get('created_at')),
            read_by=couchdb_data.get('read_by'),
        )

    except HTTPException as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving message: {str(e)}"
        )

