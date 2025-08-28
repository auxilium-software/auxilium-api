from pydantic import BaseModel


class UserLoginRequestModel(BaseModel):
    email_address: str
    raw_password: str
    recaptcha_token: str
