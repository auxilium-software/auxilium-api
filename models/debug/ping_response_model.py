from typing import List, Any, Dict

from pydantic import BaseModel


class PingResponseModel(BaseModel):
    response: str
