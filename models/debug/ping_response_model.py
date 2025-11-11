from typing import List, Any, Dict

from pydantic import BaseModel, Field


class PingResponseModel(BaseModel):
    response: str = Field(..., description="Simple ping response")
