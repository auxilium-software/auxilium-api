from enum import Enum


class DatabaseObjectType(Enum):
    USER            = "/auxilium/3/database_object/couchdb/user"
    CASE            = "/auxilium/3/database_object/couchdb/case"
    CASE_TODO_ITEM  = "/auxilium/3/database_object/couchdb/case/todo_item"
    FILE            = "/auxilium/3/database_object/couchdb/file"
    MESSAGE         = "/auxilium/3/database_object/couchdb/message"
    TIMELINE_ITEM   = "/auxilium/3/database_object/couchdb/timeline_item"
