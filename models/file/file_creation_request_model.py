from datetime import datetime
from typing import Any

from fastapi import File
from pydantic import EmailStr, BaseModel


class FileCreationRequestModel(BaseModel):
    file: Any = File(...)
    description: str
