
import logging

from fastapi import HTTPException, Depends, status, APIRouter

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.databases.rabbitmq_interactions import get_rabbitmq_dependency, publish_message
from common.rabbitmq_message_builder import RabbitMQMessageBuilder
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/tests", tags=["System Tests"])


@router.post(
    path="/test-email",
    response_model=SuccessResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "System Tests",
    ]
)
async def send_test_email_to_user(
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not current_user.is_admin:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail=f"Only admins may access System Tests."
            )

        mariadb_data, couchdb_data = get_user_details(
            current_user.id, mariadb, couchdb, configuration
        )

        publish_message(
            connection=rabbitmq,
            queue_key="Notifications",
            message=RabbitMQMessageBuilder.build_email(
                configuration=configuration,
                recipient_address=mariadb_data.email_address,
                recipient_name=couchdb_data['full_name'],
                subject="Test Email",
                txtBody="""
Test Email
==========
This email confirms the email system is working correctly.
""",
                htmlBody="""
<h1>Test Email</h1>
</hr>
<p>
    This email confirms the email system is working correctly.
</p>
"""
            ),
        )

        return SuccessResponseModel()

    except HTTPException as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to send test email: {str(e)}"
        )
