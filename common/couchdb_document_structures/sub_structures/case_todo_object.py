from datetime import datetime
from typing import List

from enumerators.todo_priority import TodoPriority
from enumerators.todo_status import TodoStatus


class CaseTodoObject:
    created_at:     datetime
    created_by:     str

    summary:        str
    description:    str
    status:         TodoStatus
    priority:       TodoPriority

    due_date:       datetime|None
    completed_at:   datetime|None
    assigned_to:    str|None

    reminders:      list


    def set_required_properties(
            self,

            created_at: datetime,
            created_by: str,

            summary: str,
            description: str,
            status: TodoStatus,
            priority: TodoPriority,

            due_date: datetime|None,
            completed_at: datetime|None,
            assigned_to: str|None,
    ):
        self.created_at     = created_at
        self.created_by     = created_by

        self.summary        = summary
        self.description    = description
        self.status         = status
        self.priority       = priority

        self.due_date       = due_date
        self.completed_at   = completed_at
        self.assigned_to    = assigned_to


    def set_reminder(self):
        self.reminders = []


    def to_json(self):
        return {
            'created_at':   self.created_at.isoformat(),
            'created_by':   self.created_by,

            'summary':      self.summary,
            'description':  self.description,
            'status':       self.status,
            'priority':     self.priority,

            'due_date':     self.due_date.isoformat(),
            'completed_at': self.completed_at.isoformat(),
            'assigned_to':  self.assigned_to,

            'reminders':    self.reminders,
        }
