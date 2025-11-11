
from pydantic import BaseModel, Field


class PasswordUpdateRequestModel(BaseModel):
    current_password:   str = Field(..., description="What the current password is,")
    new_password:       str = Field(..., description="What the password should be set to.")
