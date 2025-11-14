import logging
from datetime import datetime

from fastapi import HTTPException, Depends, APIRouter, Path
from fastapi import status as http_status

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.utilities.case_utilities import get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.message_utilities import get_message_details
from common.utilities.security_utilities import get_current_user
from common.uuid_handling import UUIDHandling
from models.message.message_response_model import MessageResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/messages", tags=["Messages"])



@router.get(
    path="/{message_id:path}",
    response_model=MessageResponseModel,
    status_code=http_status.HTTP_200_OK,
    tags=[
        "Messages",
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
