from pydantic import BaseModel


class AddPersonRequestModel(BaseModel):
    user_id: str
