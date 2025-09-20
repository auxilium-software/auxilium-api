import logging
from typing import Optional, List, Dict, Any

from fastapi import HTTPException, Depends, status, APIRouter, Query, Path
from pydantic import BaseModel

from common.databases.couchdb_interactions import get_couchdb_connection
from common.databases.mariadb_interactions import get_mariadb_connection
from common.utilities.case_utilities import get_cases_with_filter, get_cases_collection, build_case_response, DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE
from common.utilities.configuration import get_configuration
from common.utilities.security_utilities import (
    get_current_user
)
from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_case_response_model import PaginatedCasesResponse

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases", tags=["Cases"])


def pagination_params(
        page: int = Query(1, ge=1, description="Page number"),
        per_page: Optional[int] = Query(None, ge=1, le=MAX_PAGE_SIZE, description="Items per page"),
) -> Dict[str, Any]:
    return {
        'page': page,
        'page_size': per_page or DEFAULT_PAGE_SIZE
    }


def filter_params(
        search: Optional[str] = Query(None, description="Search in title, description, brief_description"),
        status: Optional[str] = Query(None, description="Filter by case status"),
        priority: Optional[str] = Query(None, description="Filter by priority level"),
) -> Dict[str, Any]:
    return {
        'search': search,
        'status': status,
        'priority': priority
    }


def sort_params(
        sort: str = Query("created_at", description="Field to sort by"),
        order: str = Query("desc", regex="^(asc|desc)$", description="Sort order"),
) -> Dict[str, str]:
    return {
        'sort': sort,
        'order': order.lower()
    }



@router.get("/mine", response_model=PaginatedCasesResponse)
async def get_my_cases(
        pagination=Depends(pagination_params),
        filters=Depends(filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        selector = {
            "clients": {
                "$elemMatch": {
                    "$eq": current_user.id
                },
            },
        }

        return await get_cases_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
        )
    except Exception as e:
        logger.error(f"Error fetching user cases: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.get("/assigned", response_model=PaginatedCasesResponse)
async def get_assigned_cases(
        pagination=Depends(pagination_params),
        filters=Depends(filter_params),
        sorting=Depends(sort_params),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        selector = {
            "workers": {
                "$elemMatch": {
                    "$eq": current_user.id
                },
            },
        }

        return await get_cases_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
        )
    except Exception as e:
        logger.error(f"Error fetching assigned cases: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.get("/all", response_model=PaginatedCasesResponse)
async def get_all_cases(
        pagination=Depends(pagination_params),
        filters=Depends(filter_params),
        sorting=Depends(sort_params),
        assigned_to: Optional[str] = Query(None, description="Filter by worker ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        mariadb=Depends(get_mariadb_connection),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        if current_user.is_admin:
            selector = {}
        else:
            selector = {
                "$or": [
                    {
                        "clients": {
                            "$elemMatch": {
                                "$eq": current_user.id
                            }
                        }
                    },
                    {
                        "workers": {
                            "$elemMatch": {
                                "$eq": current_user.id
                            }
                        }
                    }
                ]
            }

        if assigned_to:
            worker_filter = {
                "workers": {
                    "$elemMatch": {
                        "$eq": assigned_to
                    }
                }
            }

            if current_user.is_admin or not selector:
                selector.update(worker_filter)
            else:
                selector = {
                    "$and": [
                        selector,
                        worker_filter
                    ]
                }

        return await get_cases_with_filter(
            selector=selector,
            **pagination,
            **filters,
            **sorting,
            config=configuration,
            couchdb=couchdb
        )
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error fetching all cases: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch cases: {str(e)}"
        )


@router.get("/{case_id}", response_model=CaseResponseModel)
async def get_single_case(
        case_id: str = Path(..., description="Case ID"),
        configuration=Depends(get_configuration),
        current_user=Depends(get_current_user),
        couchdb=Depends(get_couchdb_connection),
):
    try:
        collection = get_cases_collection(configuration, couchdb)

        try:
            doc = collection[case_id]
        except:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Case not found"
            )

        user_id = current_user.id
        is_client = user_id in doc.get('clients', [])
        is_worker = user_id in doc.get('workers', [])
        is_admin = current_user.is_admin

        if not (is_client or is_worker or is_admin):
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="You don't have access to this case"
            )

        return build_case_response(doc)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error fetching case {case_id}: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to fetch case: {str(e)}"
        )
