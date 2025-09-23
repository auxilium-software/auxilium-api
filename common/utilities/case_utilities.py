import logging
import traceback
from typing import Optional, List, Dict, Any
from datetime import datetime

from fastapi import HTTPException, Query
from fastapi import status as http_status

from common.utilities.parameters import MAX_FETCH_LIMIT
from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_cases_response_model import PaginatedCasesResponse

logger = logging.getLogger(__name__)


def get_cases_collection(configuration, couchdb):
    db_name = configuration.get_string('Databases', 'CouchDB', 'Databases', 'Cases')
    return couchdb[db_name]


def get_single_case_and_handle_permissions(configuration, couchdb, current_user, case_id):
    collection = get_cases_collection(configuration, couchdb)

    try:
        doc = collection[case_id]
    except:
        raise HTTPException(
            status_code=http_status.HTTP_404_NOT_FOUND,
            detail="Case not found"
        )

    user_id = current_user.id
    is_client = user_id in doc.get('clients', [])
    is_worker = user_id in doc.get('workers', [])
    is_admin = current_user.is_admin

    if not (is_client or is_worker or is_admin):
        raise HTTPException(
            status_code=http_status.HTTP_403_FORBIDDEN,
            detail="You don't have access to this case"
        )

    return doc


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
        todos=doc.get('todos', []),
        timeline=doc.get('timeline', []),
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

        logger.info(f"Query selector: {query_selector}")

        skip = (page - 1) * page_size

        count_result = database.find(
            selector=query_selector,
            fields=['_id'],
            limit=MAX_FETCH_LIMIT
        )

        count_docs = count_result.get('docs', [])
        total = len(count_docs)
        logger.info(f"Count query returned {total} documents")

        result = database.find(
            selector=query_selector,
            limit=page_size,
            skip=skip,
        )

        logger.info(f"Main query returned {len(result.get('docs', []))} documents")
        if result.get('warning'):
            logger.warning(f"Query warning (ignoring): {result.get('warning')}")

        docs = result.get('docs', [])

        total_pages = (total + page_size - 1) // page_size if page_size > 0 else 0
        has_more = page < total_pages

        cases = [build_case_response(doc) for doc in docs]

        logger.info(f"Returning {len(cases)} cases out of {total} total")

        return PaginatedCasesResponse(
            data=cases,
            page=page,
            per_page=page_size,
            total=total,
            total_pages=total_pages,
            has_more=has_more
        )

    except Exception as e:
        logger.error(f"Error querying cases: {str(e)}")
        import traceback
        logger.error(f"Traceback: {traceback.format_exc()}")
        raise HTTPException(
            status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Database query failed: {str(e)}"
        )
