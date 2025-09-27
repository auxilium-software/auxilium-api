from typing import Dict, Optional, Any

from fastapi import Query


DEFAULT_PAGE_SIZE = 8
MAX_PAGE_SIZE = 1000
MAX_FETCH_LIMIT = 10000

def pagination_params(
        page: int = Query(1, ge=1, description="Page number"),
        per_page: Optional[int] = Query(None, ge=1, le=MAX_PAGE_SIZE, description="Items per page"),
) -> Dict[str, Any]:
    return {
        'page': page,
        'page_size': per_page or DEFAULT_PAGE_SIZE
    }


def case_filter_params(
        search: Optional[str] = Query(None, description="Search in title, description, brief_description"),
        status: Optional[str] = Query(None, description="Filter by case status"),
        priority: Optional[str] = Query(None, description="Filter by priority level"),
) -> Dict[str, Any]:
    return {
        'search': search,
        'status': status,
        'priority': priority
    }


def user_filter_params(
        search: Optional[str] = Query(None, description="Search in name"),
) -> Dict[str, Any]:
    return {
        'search': search,
    }


def sort_params(
        sort: str = Query("created_at", description="Field to sort by"),
        order: str = Query("desc", regex="^(asc|desc)$", description="Sort order"),
) -> Dict[str, str]:
    return {
        'sort': sort,
        'order': order.lower()
    }
