from typing import List

from pydantic import BaseModel

from models.cases.case_response_model import CaseResponseModel
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel


class PaginatedUsersResponse(BaseModel):
    data: List[SimplifiedUserDetailsResponseModel]
    page: int
    per_page: int
    total: int
    total_pages: int
    has_more: bool
