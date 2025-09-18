from pydantic import EmailStr, BaseModel


class SimplifiedUserDetailsResponseModel(BaseModel):
    id: str
    email_address: EmailStr | None
    full_name: str
    is_admin: bool
