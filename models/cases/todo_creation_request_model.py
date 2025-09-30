from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field

from enumerators.todo_priority import TodoPriority


class TodoCreationRequestModel(BaseModel):
    summary: str = Field(..., min_length=1, max_length=500, description="Todo title/summary")
    description: Optional[str] = Field(None, max_length=5000, description="Detailed description")
    priority: Optional[TodoPriority] = Field(TodoPriority.MEDIUM, description="Priority level")
    due_date: Optional[datetime] = Field(None, description="Due date for the todo")
    assigned_to: Optional[str] = Field(None, description="User ID of assignee")
    reminder: Optional[datetime] = Field(None, description="When to send reminder")
