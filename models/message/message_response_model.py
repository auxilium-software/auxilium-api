from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field


class MessageResponseModel(BaseModel):
    id:         str         = Field(..., description="Message ID")
    subject:    str         = Field(..., description="Message subject")
    content:    str         = Field(..., description="Message content")
    sender_id:  str         = Field(..., description="Sender user ID")
    is_urgent:  bool        = Field(False, description="Whether this is an urgent message")
    is_read:    bool        = Field(False, description="Whether the message has been read")
    created_at: datetime    = Field(..., description="When the message was created")
    read_by:    dict        = Field(..., description="Who read the message and when")
