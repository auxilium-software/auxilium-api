from datetime import datetime
from typing import Optional

from pydantic import BaseModel

from enumerators.todo_priority import TodoPriority
from enumerators.todo_status import TodoStatus


class TodoResponseModel(BaseModel):
    id:             str
    case_id:        str
    summary:        str
    description:    Optional[str]
    status:         TodoStatus
    priority:       TodoPriority
    created_at:     datetime
    created_by:     str
    due_date:       Optional[datetime]
    completed_at:   Optional[datetime]
    assigned_to:    Optional[str]
