from typing import Optional, List, Dict, Any
from datetime import datetime

from fastapi import HTTPException, Query
from fastapi import status as http_status

from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_case_response_model import PaginatedCasesResponse

DEFAULT_PAGE_SIZE = 8
MAX_PAGE_SIZE = 100
MAX_FETCH_LIMIT = 10000


def get_cases_collection(config, couchdb):
    db_name = config.get_string('Databases', 'CouchDB', 'Databases', 'Cases')
    return couchdb[db_name]


def build_case_response(doc: Dict) -> CaseResponseModel:
    return CaseResponseModel(
        id=doc['_id'],
        sensitivity=doc.get('sensitivity'),
        title=doc.get('title'),
        status=doc.get('status'),
        brief_description=doc.get('brief_description'),
        case_referrer=doc.get('case_referrer'),
        description=doc.get('description'),
        workers=doc.get('workers', []),
        clients=doc.get('clients', []),
        additional_properties=doc.get('additional_properties', {}),
    )


async def get_cases_with_filter(
        selector: Dict,
        page: int,
        page_size: int,
        search: Optional[str],
        status: Optional[str],
        priority: Optional[str],
        sort: str,
        order: str,
        config,
        couchdb
) -> PaginatedCasesResponse:
    try:
        database = get_cases_collection(config, couchdb)

        query_selector = selector if selector else {}

        skip = (page - 1) * page_size

        count_result = database.find(
            selector=query_selector,
            fields=['_id'],
            limit=MAX_FETCH_LIMIT
        )

        count_docs = count_result.get('docs', [])
        total = len(count_docs)

        result = database.find(
            selector=query_selector,
            limit=page_size,
            skip=skip,
        )

        docs = result.get('docs', [])

        total_pages = (total + page_size - 1) // page_size if page_size > 0 else 0
        has_more = page < total_pages

        cases = [build_case_response(doc) for doc in docs]


        return PaginatedCasesResponse(
            data=cases,
            page=page,
            per_page=page_size,
            total=total,
            total_pages=total_pages,
            has_more=has_more
        )

    except Exception as e:
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Database query failed: {str(e)}"
        )
