from typing import List, Any, Dict

from pydantic import BaseModel


class CaseResponseModel(BaseModel):
    id:                     str
    title:                  str | None
    description:            str | None
    sensitivity:            str | None
    status:                 str | None
    case_referrer:          str | None
    additional_properties:  Dict[str, Any]
    workers:                List[str]
    clients:                List[str]
    todos:                  dict
    timeline:               dict
    messages:               List[str]
    files:                  List[str]
