from pydantic import BaseModel


class SuccessResponseModel(BaseModel):
    status: str = "SUCCESS"
