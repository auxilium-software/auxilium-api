from typing import Any, Dict

from fastapi import HTTPException
from fastapi import status

from common.couchdb_document_structures.case_document import CaseDocument
from models.cases.case_response_model import CaseResponseModel


class DocumentToolsInterface:
    @staticmethod
    def save_document(document_builder: CaseDocument):
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="function has not been overriden correctly"
        )

    @staticmethod
    def get_document(case_id: str) -> dict:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="function has not been overriden correctly"
        )

    @staticmethod
    def save_multiple_properties(self, case_id: str, properties: dict[str, Any]):
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="function has not been overriden correctly"
        )

    @staticmethod
    def build_response(doc: Dict) -> CaseResponseModel:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="function has not been overriden correctly"
        )
