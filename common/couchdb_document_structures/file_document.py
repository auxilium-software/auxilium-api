from datetime import datetime
from typing import List


class FileDocument:
    _id:            str
    _rev:           str
    created_at:     datetime
    created_by:     str
    updated_at:     datetime

    filename:       str
    description:    str
    content_type:   str
    hash:           str
    size:           int


    def set_required_properties(
            self,

            _id: str,
            created_by: str,

            filename: str,
            description: str,
            content_type: str,
            hash: str,
            size: int,
    ):
        self._id = _id
        self.created_at = datetime.now()
        self.created_by = created_by
        self.updated_at = datetime.now()

        self.filename = filename
        self.description = description
        self.content_type = content_type
        self.hash = hash
        self.size = size


    def to_json(self):
        return {
            '_id':                      self._id,
            # '_rev':                     self._rev,
            'created_at':               self.created_at.isoformat(),
            'created_by':               self.created_by,
            'updated_at':               self.updated_at.isoformat(),

            'filename':     self.filename,
            'description':  self.description,
            'content_type': self.content_type,
            'hash':         self.hash,
            'size':         self.size,
        }
