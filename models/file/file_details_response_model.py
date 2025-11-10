from datetime import datetime

from pydantic import EmailStr, BaseModel


class FileDetailsResponseModel(BaseModel):
    id:             str
    filename:       str
    content_type:   str
    hash:           str
    size:           int
    uploaded_at:    datetime
    uploaded_by:    str
    contents:       str
