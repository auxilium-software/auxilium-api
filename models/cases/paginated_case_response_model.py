from typing import List

from pydantic import BaseModel

from models.cases.case_response_model import CaseResponseModel


class PaginatedCasesResponse(BaseModel):
    data: List[CaseResponseModel]
    page: int
    per_page: int
    total: int
    total_pages: int
    has_more: bool
