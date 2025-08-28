from typing import Optional

from pydantic import EmailStr, BaseModel


class UserRegistrationRequestModel(BaseModel):
    recaptcha_token: str

    on_behalf_of: str
    data_processing_consent: str
    full_name: str
    telephone_number: str
    full_address: str
    gender: str
    ethnic_group: str
    date_of_birth: str
    how_did_you_find_out_about_our_service: str
    email_address: str
    raw_password: str
    case_description: str
