import logging
from datetime import datetime
from typing import Dict, List, Any, Optional

from fastapi import HTTPException
from sqlalchemy import text
from fastapi import status as http_status

from common.utilities.parameters import MAX_FETCH_LIMIT
from models.user.paginated_users_response_model import PaginatedUsersResponse
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel
from models.user.user_details_response_model import UserDetailsResponseModel

logger = logging.getLogger(__name__)


def get_user_details(user_id: str, mariadb, couchdb, config):
    result = mariadb.execute(
        text("SELECT * FROM users WHERE id = :id"),
        {"id": user_id}
    )
    mariadb_data = result.fetchone()

    if not mariadb_data:
        return None, None

    couchdb_data = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Users')].get(user_id)
    return mariadb_data, couchdb_data


def find_shared_cases(user_id: str, other_user_id: str, couchdb, config) -> List[str]:
    cases_collection = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Cases')]

    query = {
        "selector": {
            "$or": [
                {"clients": {"$elemMatch": {"$eq": user_id}}},
                {"workers": {"$elemMatch": {"$eq": user_id}}}
            ]
        }
    }

    user_cases = list(cases_collection.find(query))

    shared_cases = []
    for case in user_cases:
        other_is_client = other_user_id in case.get('clients', [])
        other_is_worker = other_user_id in case.get('workers', [])

        if other_is_client or other_is_worker:
            shared_cases.append(case['_id'])

    return shared_cases


def check_user_access(current_user, target_user_id: str, couchdb, config) -> bool:
    if current_user.id == target_user_id:
        return True

    if current_user.is_admin:
        return True

    shared_cases = find_shared_cases(current_user.id, target_user_id, couchdb, config)
    return len(shared_cases) > 0


def get_user_properties(user_id: str, couchdb, config) -> Dict:
    users_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Users')]
    user_doc = users_db.get(user_id)

    if not user_doc:
        return {}

    system_props = ['_id', '_rev', 'full_name', 'email_address', 'created_at', 'updated_at']
    properties = {}

    for key, value in user_doc.get('additional_properties').items():
        if key not in system_props and not key.startswith('_'):
            properties[key] = value

    return properties


def save_user_property(user_id: str, property_name: str, property_value: Any, couchdb, config):
    users_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Users')]
    user_doc = users_db.get(user_id)

    if not user_doc:
        raise HTTPException(
            status_code=http_status.HTTP_404_NOT_FOUND,
            detail="User does not exist"
        )

    user_doc['additional_properties'][property_name] = property_value
    user_doc['last_updated_at'] = datetime.utcnow().isoformat()

    users_db.save(user_doc)
    return user_doc




















def get_users_collection(config, couchdb):
    db_name = config.get_string('Databases', 'CouchDB', 'Databases', 'Users')
    return couchdb[db_name]


def build_users_response(doc: Dict) -> SimplifiedUserDetailsResponseModel:
    return SimplifiedUserDetailsResponseModel(
        id=doc['_id'],
        email_address="example@example.example",
        full_name=doc.get('full_name'),
        is_admin=False,
    )


async def get_users_with_filter(
        selector: Dict,
        page: int,
        page_size: int,
        search: Optional[str],
        sort: str,
        order: str,
        config,
        couchdb
) -> PaginatedUsersResponse:
    try:
        database = get_users_collection(config, couchdb)

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

        if result.get('warning'):
            logger.warning(f"Query warning (ignoring): {result.get('warning')}")

        docs = result.get('docs', [])

        total_pages = (total + page_size - 1) // page_size if page_size > 0 else 0
        has_more = page < total_pages

        users = [build_users_response(doc) for doc in docs]

        return PaginatedUsersResponse(
            data=users,
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
