from typing import List, Any, Dict

from pydantic import BaseModel


class CaseCreationRequestModel(BaseModel):
    title:          str | None
    description:    str | None
