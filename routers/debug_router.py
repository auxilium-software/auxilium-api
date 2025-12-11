import logging

from fastapi import HTTPException, status, APIRouter

from common.utilities.logging_utilities import PRIMARY_LOGGER
from models.debug.ping_response_model import PingResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/server", tags=["Debug"])

@router.get(
    path="/ping",
    response_model=PingResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Debug",
    ]
)
async def ping(
        # configuration=Depends(get_configuration),
        # current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        # couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        return PingResponseModel(
            response="pong!"
        )

    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to fetch user details"
        )
