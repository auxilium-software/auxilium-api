from datetime import datetime
from typing import List


class UserDocument:
    _id:                    str
    _rev:                   str
    created_at:             datetime
    created_by:             str
    updated_at:             datetime

    full_name:              str
    full_address:           str
    telephone_number:       str
    gender:                 str
    date_of_birth:          str

    additional_properties:  dict        = {}
    files:                  List[str]   = []
    migrations:             dict        = {}

    how_did_you_find_out_about_our_service: str|None = None


    def set_required_properties(
            self,

            _id: str,
            created_by: str,

            full_name: str,
            full_address: str,
            telephone_number: str,
            gender: str,
            date_of_birth: str,
    ):
        self._id = _id
        self.created_at = datetime.now()
        self.created_by = created_by
        self.updated_at = datetime.now()

        self.full_name          = full_name
        self.full_address       = full_address
        self.telephone_number   = telephone_number
        self.gender             = gender
        self.date_of_birth      = date_of_birth


    def to_json(self):
        return {
            '_id':                      self._id,
            # '_rev':                     self._rev,
            'created_at':               self.created_at.isoformat(),
            'created_by':               self.created_by,
            'updated_at':               self.updated_at.isoformat(),

            'full_name':                self.full_name,
            'full_address':             self.full_address,
            'telephone_number':         self.telephone_number,
            'gender':                   self.gender,
            'date_of_birth':            self.date_of_birth,

            'additional_properties':    self.additional_properties,
            'files':                    self.files,
            'migrations':               self.migrations,

            'how_did_you_find_out_about_our_service': self.how_did_you_find_out_about_our_service,
        }
