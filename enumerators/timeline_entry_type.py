from enum import Enum


class TimelineEntryType(Enum):
    TODO_STATUS_UPDATE  = "/timeline/status-update"
    TODO_COMPLETION     = "/timeline/completion"
