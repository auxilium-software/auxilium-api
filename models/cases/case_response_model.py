from typing import List, Any, Dict

from pydantic import BaseModel


class CaseResponseModel(BaseModel):
    id: str
    sensitivity: str | None
    title: str | None
    status: str | None
    brief_description: str | None
    case_referrer: str | None
    description: str | None
    additional_properties: Dict[str, Any] = {}
    workers: List[str] = []
    clients: List[str] = []
    todos: List[str] = []
    timeline: List[str] = []
