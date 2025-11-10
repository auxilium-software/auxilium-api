
from pydantic import BaseModel


class PasswordUpdateRequestModel(BaseModel):
    current_password:   str
    new_password:       str
