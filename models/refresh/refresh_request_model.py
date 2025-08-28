from pydantic import BaseModel


class RefreshRequestModel(BaseModel):
    refresh_token: str
