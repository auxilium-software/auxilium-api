from datetime import datetime
from typing import List


class FileDocument:
    _id:            str
    _rev:           str
    filename:       str
    description:    str
    content_type:   str
    hash:           str
    size:           int
    uploaded_at:    datetime
    uploaded_by:    str

    def to_json(self):
        return {
            '_id':          self._id,
            '_rev':         self._rev,
            'filename':     self.filename,
            'description':  self.description,
            'content_type': self.content_type,
            'hash':         self.hash,
            'size':         self.size,
            'uploaded_at':  self.uploaded_at,
            'uploaded_by':  self.uploaded_by,
        }
