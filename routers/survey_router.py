import logging

from fastapi import HTTPException, Depends, APIRouter
from fastapi import status as http_status

from common.couchdb_document_structures.enumerators.survey_type_enum import SurveyTypeEnum
from common.couchdb_document_structures.survey_document import SurveyDocument
from common.utilities.configuration_utilities import get_configuration
from common.utilities.logging_utilities import PRIMARY_LOGGER
from common.utilities.security_utilities import get_current_user
from models.success_response_model import SuccessResponseModel
from models.survey.screening_unmet_legal_needs_in_veterans_for_professionals_and_caseworkers.survey_results_model import \
    SurveyResultsModel

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/surveys", tags=["Surveys"])

@router.post(
    path="/screening-unmet-legal-needs-in-veterans-for-professionals-and-caseworkers",
    response_model=SuccessResponseModel,
    status_code=http_status.HTTP_200_OK,
    tags=[
        "Messages",
    ]
)
async def get_single_message_from_case(
        request: SurveyResultsModel,
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        # mariadb=Depends(get_mariadb_dependency),
        # couchdb=Depends(get_couchdb_dependency),
        # redis=Depends(get_redis_dependency),
        # rabbitmq=Depends(get_rabbitmq_dependency),
):
    try:
        print(request)

        doc_builder = SurveyDocument()
        doc_builder.type = SurveyTypeEnum.SCREENING_UNMET_LEGAL_NEEDS_IN_VETERANS_FOR_PROFESSIONALS_AND_CASEWORKERS
        doc_builder.data = request

        # return SuccessResponseModel()
    except HTTPException as e:
        # mariadb.rollback()
        PRIMARY_LOGGER.exception(e)
        raise e
    except Exception as e:
        PRIMARY_LOGGER.exception(e)
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Error retrieving message: {str(e)}"
        )
