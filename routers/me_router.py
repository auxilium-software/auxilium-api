import logging
from datetime import datetime

from fastapi import HTTPException, Depends, APIRouter, Path, Body
from fastapi import status as http_status
from sqlalchemy import text
from starlette.status import HTTP_200_OK

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.password_helpers import get_password_hash
from common.utilities.case_utilities import get_single_case_and_handle_permissions
from common.utilities.configuration import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.message_utilities import get_message_details
from common.utilities.security_utilities import get_current_user
from common.utilities.user_utilities import check_user_access
from common.uuid_handling import UUIDHandling
from models.me.password_update_request_model import PasswordUpdateRequestModel
from models.message.message_response_model import MessageResponseModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/me", tags=["Account Management"])

@router.post(
    path="/change-password",
    response_model=SuccessResponseModel,
    status_code=HTTP_200_OK,
    tags=[
        "Account Management",
    ]
)
async def change_password(
        request: PasswordUpdateRequestModel,
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not check_user_access(current_user, current_user.id, couchdb, configuration):
            raise HTTPException(
                status_code=http_status.HTTP_403_FORBIDDEN,
                detail="You don't have permission to modify this user's properties"
            )

        if request.current_password == request.new_password:
            raise HTTPException(
                status_code=http_status.HTTP_409_CONFLICT,
                detail="New password may not be the same as the old password."
            )

        new_password_hash = get_password_hash(request.new_password)

        mariadb.execute(
            text("""
                UPDATE users
                SET password_hash = :new_password_hash
                WHERE id = :user_id;
            """),
            {
                "new_password_hash": new_password_hash,
                "user_id": current_user.id,
            }
        )
        mariadb.commit()

        return SuccessResponseModel()

    except HTTPException as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error creating todo item for case: {str(e)}"
        )
