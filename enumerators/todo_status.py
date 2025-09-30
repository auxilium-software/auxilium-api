from enum import Enum


class TodoStatus(str, Enum):
    NEEDS_ACTION = "NEEDS-ACTION"
    IN_PROGRESS = "IN-PROCESS"
    COMPLETED = "COMPLETED"
    CANCELLED = "CANCELLED"
