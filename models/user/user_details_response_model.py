from datetime import datetime

from pydantic import EmailStr, BaseModel


class UserDetailsResponseModel(BaseModel):
    id: str
    email_address: EmailStr | None
    full_name: str
    is_admin: bool
    additional_properties: dict  # [str, dict[str, str|int|float|bool|None]]
    documents: list
    created_at: datetime
    last_updated_at: str
