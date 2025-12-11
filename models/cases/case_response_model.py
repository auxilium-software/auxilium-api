from datetime import datetime
from typing import List, Any, Dict

from pydantic import BaseModel


class CaseResponseModel(BaseModel):
    id:                     str
    created_at:             datetime
    created_by:             str
    last_updated_at:        datetime
    last_updated_by:        str

    title:                  str
    description:            str

    sensitivity:            str
    status:                 str

    referrer:               str|None                = None
    workers:                List[str]               = []
    clients:                List[str]               = []
    todos:                  dict                    = {}
    timeline:               dict                    = {}
    messages:               List[str]               = []

    additional_properties:  dict                    = {}
    files:                  List[str]               = []

    # migrations:             dict                    = {}
