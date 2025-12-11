from datetime import datetime
from typing import List

from pydantic import EmailStr, BaseModel


class UserDetailsResponseModel(BaseModel):
    id:                     str

    created_at:             datetime
    created_by:             str
    last_updated_at:        datetime
    last_updated_by:        str

    full_name:              str
    full_address:           str
    telephone_number:       str
    gender:                 str
    date_of_birth:          str

    additional_properties:  dict        = {}
    files:                  List[str]   = []

    how_did_you_find_out_about_our_service: str|None = None


    # properties stored in mariadb
    email_address:          str|None = None
    is_admin:               bool|None = None
