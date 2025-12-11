import logging
import mimetypes

from fastapi import HTTPException, Depends, APIRouter, Form, File, UploadFile, Path
from fastapi import status as http_status
from sqlalchemy import text

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.document_modification.file_document_tools import create_file
from common.document_modification.user_document_tools import UserDocumentTools
from common.password_helpers import get_password_hash
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from common.uuid_handling import UUIDHandling
from enumerators.property_type import PropertyType
from models.me.password_update_request_model import PasswordUpdateRequestModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/me", tags=["Account Management"])


@router.post(
    path="/upload",
    response_model=SuccessResponseModel,
    status_code=http_status.HTTP_200_OK,
    tags=[
        "Users",
    ]
)
async def upload_file(
        file: UploadFile = File(...),
        description: str = Form(...),
        user_id: str = Path(..., description="User ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not UUIDHandling.is_valid(user_id):
            raise HTTPException(
                status_code=http_status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        doc_tools = UserDocumentTools(
            configuration=configuration,
            couchdb=couchdb,
            mariadb=mariadb,
            current_user=current_user,
        )

        content = await file.read()
        content_type = file.content_type or mimetypes.guess_type(file.filename)[0] or PropertyType.BINARY

        create_file(
            document_type="Users",
            document_id=user_id,
            file_name=file.filename,
            file_type=content_type,
            uploaded_by=current_user.id,
            file_contents=content.hex() if isinstance(content, bytes) else content,
            description=description,
            couchdb=couchdb,
            config=configuration,
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
            detail="Failed to upload file"
        )


@router.post(
    path="/change-password",
    response_model=SuccessResponseModel,
    status_code=http_status.HTTP_200_OK,
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
