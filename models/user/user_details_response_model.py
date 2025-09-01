from pydantic import EmailStr, BaseModel


class UserDetailsResponseModel(BaseModel):
    id: str
    email_address: EmailStr
    full_name: str
    is_admin: bool
