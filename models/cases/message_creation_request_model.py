from typing import Optional
from pydantic import BaseModel, Field


class MessageCreationRequestModel(BaseModel):
    subject:    str     = Field(..., description="Message subject", min_length=1, max_length=200)
    content:    str     = Field(..., description="Message content", min_length=1)
    is_urgent:  bool    = Field(False, description="Whether this is an urgent message")
