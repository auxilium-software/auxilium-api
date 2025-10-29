from datetime import datetime
from typing import Optional
from pydantic import BaseModel, Field


class TimelineEntryResponseModel(BaseModel):
    id:         str         = Field(..., description="Timeline Entry ID")
