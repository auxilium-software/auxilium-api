from datetime import datetime
from typing import List


class MessageDocument:
    _id:        str
    _rev:       str
    created_at: str
    subject:    str
    content:    str
    sender_id:  str
    is_urgent:  int
    read_by:    dict
    updated_at: str

    def to_json(self):
        return {
            '_id':          self._id,
            '_rev':         self._rev,
            'created_at':   self.created_at,
            'subject':      self.subject,
            'content':      self.content,
            'sender_id':    self.sender_id,
            'is_urgent':    self.is_urgent,
            'read_by':      self.read_by,
            'updated_at':   self.updated_at,
        }
