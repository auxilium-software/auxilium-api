from enum import Enum


class PropertyType(str, Enum):
    TEXT = "text/plain"
    JSON = "application/json"
    ICALENDAR = "text/calendar"
    HTML = "text/html"
    MARKDOWN = "text/markdown"
    BINARY = "application/octet-stream"
