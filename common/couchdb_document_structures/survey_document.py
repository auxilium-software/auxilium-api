from datetime import datetime
from typing import List

from common.couchdb_document_structures.enumerators.survey_type_enum import SurveyTypeEnum


class SurveyDocument:
    _id:                    str
    _rev:                   str
    created_at:             datetime
    created_by:             str
    updated_at:             datetime

    type:   SurveyTypeEnum
    data:   dict


    def set_required_properties(
            self,

            _id: str,
            created_by: str,

            type: SurveyTypeEnum,
            data: dict,
    ):
        self._id = _id
        self.created_at = datetime.now()
        self.created_by = created_by
        self.updated_at = datetime.now()

        self.type       = type
        self.data       = data


    def to_json(self):
        return {
            '_id':          self._id,
            # '_rev':       self._rev,
            'created_at':   self.created_at,
            'created_by':   self.created_by,
            'updated_at':   self.updated_at,

            'type':         self.type.value,
            'data':         self.data,
        }
