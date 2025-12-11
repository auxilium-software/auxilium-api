import logging

from fastapi import HTTPException, Depends, status, APIRouter, Path
from fastapi.responses import Response

from common.databases.couchdb_interactions import get_couchdb_dependency
from common.databases.mariadb_interactions import get_mariadb_dependency
from common.utilities.configuration_utilities import get_configuration
from common.document_modification.file_document_tools import get_file_details, get_file_contents, delete_file_from_lfs
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from common.uuid_handling import UUIDHandling
import base64

from models.file.file_details_response_model import FileDetailsResponseModel
from models.success_response_model import SuccessResponseModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/files", tags=["Files"])


@router.get(
    path="/{file_id:path}/render",
    status_code=status.HTTP_200_OK,
    tags=["Files"],
    response_class=Response,
)
async def render_file(
        file_id: str = Path(..., description="File ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
):
    try:
        if not UUIDHandling.is_valid(file_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        if not current_user.is_admin:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="Insufficient permissions"
            )

        _, couchdb_data = get_file_details(
            file_id, mariadb, couchdb, configuration
        )

        file_data = get_file_contents(couchdb_data.get('_id'), configuration)

        if isinstance(file_data, str):
            file_data = base64.b64decode(file_data)

        file_size = len(file_data)
        content_type = couchdb_data.get('content_type', 'application/octet-stream')
        filename = couchdb_data.get('filename', 'file')

        return Response(
            content=file_data,
            status_code=status.HTTP_200_OK,
            headers={
                'Accept-Ranges': 'bytes',
                'Content-Length': str(file_size),
                'Content-Type': content_type,
                'Content-Disposition': f'inline; filename="{filename}"',
                'Cache-Control': 'public, max-age=3600',
            },
            media_type=content_type,
        )

    except HTTPException as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to render file: {str(e)}"
        )


@router.get(
    path="/{file_id:path}",
    response_model=FileDetailsResponseModel,
    status_code=status.HTTP_200_OK,
    tags=[
        "Files",
    ]
)
async def get_file(
        file_id: str = Path(..., description="File ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_dependency),
        couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        if not UUIDHandling.is_valid(file_id):
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="You must provide a UUID."
            )

        if current_user.is_admin:
            selector = {}
        else:
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"wip lol"
            )

        _, couchdb_data = get_file_details(
            file_id, mariadb, couchdb, configuration
        )

        return FileDetailsResponseModel(
            id=couchdb_data.get('_id'),
            filename=couchdb_data.get('filename'),
            content_type=couchdb_data.get('content_type'),
            hash=couchdb_data.get('hash'),
            size=couchdb_data.get('size'),
            uploaded_at=couchdb_data.get('uploaded_at'),
            uploaded_by=couchdb_data.get('uploaded_by'),
        )

    except HTTPException as e:
        mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


