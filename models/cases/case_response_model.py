from typing import List, Any, Dict

from pydantic import EmailStr, BaseModel


class CaseResponseModel(BaseModel):
    id: str
    sensitivity: str
    title: str
    status: str
    brief_description: str
    case_referrer: str
    description: str
    workers: List[str] = []
    clients: List[str] = []
    additional_properties: Dict[str, Any] = {}

