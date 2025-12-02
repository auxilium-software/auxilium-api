from typing import List, Any, Dict

from pydantic import BaseModel, Field


class CaseUpdateRequestModel(BaseModel):
    title:          str | None  = Field(None, description="The Case Title (short summary for the Case)")
    description:    str | None  = Field(None, description="The Case Description")
