from datetime import datetime
from typing import List

from common.couchdb_document_structures.enumerators.case_sensitivity_enum import CaseSensitivityEnum
from common.couchdb_document_structures.enumerators.case_status_enum import CaseStatusEnum


class CaseDocument:
    _id:                    str
    _rev:                   str
    created_at:             datetime
    created_by:             str
    updated_at:             datetime

    title:                  str
    description:            str

    sensitivity:            CaseSensitivityEnum
    status:                 CaseStatusEnum

    referrer:               str                     = None
    workers:                List[str]               = []
    clients:                List[str]               = []
    todos:                  dict                    = {}
    timeline:               dict                    = {}
    messages:               List[str]               = []

    additional_properties:  dict                    = {}
    files:                  List[str]               = []
    migrations:             dict                    = {}


    def set_required_properties(
            self,

            _id: str,
            created_by: str,

            title: str,
            description: str,
            sensitivity: CaseSensitivityEnum,
            status: CaseStatusEnum,
    ):
        self._id = _id
        self.created_at = datetime.now()
        self.created_by = created_by
        self.updated_at = datetime.now()

        self.title          = title
        self.description    = description
        self.sensitivity    = sensitivity
        self.status         = status

    def to_json(self):
        return {
            '_id':                      self._id,
            # '_rev':                     self._rev,
            'created_at':               self.created_at.isoformat(),
            'created_by':               self.created_by,
            'updated_at':               self.updated_at.isoformat(),

            'title':                    self.title,
            'description':              self.description,

            'sensitivity':              self.sensitivity.value,
            'status':                   self.status.value,

            'referrer':                 self.referrer,
            'workers':                  self.workers,
            'clients':                  self.clients,
            'todos':                    self.todos,
            'timeline':                 self.timeline,
            'messages':                 self.messages,

            'additional_properties':    self.additional_properties,
            'files':                    self.files,
            'migrations':               self.migrations,
        }

    def from_json(self, json_dict):
        self._id                    = json_dict['_id']
        self._rev                   = json_dict['_rev']
        self.created_at             = json_dict['created_at']
        self.created_by             = json_dict['created_by']
        self.updated_at             = json_dict['updated_at']

        self.title                  = json_dict['title']
        self.description            = json_dict['description']

        self.sensitivity            = json_dict['sensitivity']
        self.status                 = json_dict['status']

        self.referrer               = json_dict['referrer']
        self.workers                = json_dict['workers']
        self.clients                = json_dict['clients']
        self.todos                  = json_dict['todos']
        self.timeline               = json_dict['timeline']
        self.messages               = json_dict['messages']

        self.additional_properties  = json_dict['additional_properties']
        self.files                  = json_dict['files']
        self.migrations             = json_dict['migrations']
