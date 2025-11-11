from typing import List, Any, Dict

from pydantic import BaseModel, Field


class CaseCreationRequestModel(BaseModel):
    title:          str | None  = Field(..., description="The Case Title (short summary for the Case)")
    description:    str | None  = Field(..., description="The Case Description")
