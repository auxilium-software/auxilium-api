from enum import Enum


class DatabaseObjectType(Enum):
    USER = "aux3/couchdb/users"
    CASE = "aux3/couchdb/cases"
    FILE = "aux3/couchdb/files"
    MESSAGE = "aux3/couchdb/messages"
