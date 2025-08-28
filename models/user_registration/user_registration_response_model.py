from pydantic import EmailStr, BaseModel


class UserRegistrationResponseModel(BaseModel):
    id: str
    email_address: EmailStr
