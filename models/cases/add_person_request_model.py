from pydantic import BaseModel, Field


class AddPersonRequestModel(BaseModel):
    user_id: str = Field(..., description="The ID of the User to add to the Case", min_length=36, max_length=36)
