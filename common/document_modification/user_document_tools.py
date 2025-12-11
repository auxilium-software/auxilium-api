import logging
from typing import Optional, Dict, Any, List
from datetime import datetime

from fastapi import HTTPException
from fastapi import status as http_status

from common.couchdb_document_structures.user_document import UserDocument
from common.document_modification.document_tools import DocumentTools
from common.document_modification.document_tools_interface import DocumentToolsInterface
from common.parameters import MAX_FETCH_LIMIT
from models.user.paginated_users_response_model import PaginatedUsersResponse
from models.user.simplified_user_details_response_model import SimplifiedUserDetailsResponseModel

logger = logging.getLogger(__name__)


class UserDocumentTools(DocumentToolsInterface):
    def __init__(self, configuration, couchdb, mariadb, current_user):
        self.configuration = configuration
        self.couchdb = couchdb
        self.mariadb = mariadb
        self.current_user = current_user

    def save_document(self, document_builder: UserDocument):
        collection = DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Users'
        )
        collection.save(document_builder.to_json())

    def get_document(self, user_id: str) -> dict:
        doc = DocumentTools.get_document_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Users',
            document_id=user_id
        )
        return doc

    def save_multiple_properties(self, user_id: str, properties: dict[str, Any]):
        user_doc = self.get_document(user_id=user_id)

        for property_key, property_value in properties.items():
            user_doc[property_key] = property_value

        user_doc['last_updated_at'] = datetime.utcnow().isoformat()
        user_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Users'
        ).save(user_doc)

        return user_doc

    def build_response(self, doc: Dict) -> SimplifiedUserDetailsResponseModel:
        return SimplifiedUserDetailsResponseModel(
            id=doc['_id'],
            email_address=doc.get('email_address', 'example@example.example'),
            full_name=doc.get('full_name'),
            is_admin=doc.get('is_admin', False),
        )

    # ====================================================================================================
    # ADDITIONAL FUNCTIONS
    # ====================================================================================================

    async def get_users_with_filter(
            self,
            selector: Dict,
            page: int,
            page_size: int,
            search: Optional[str],
            sort: str,
            order: str,
    ) -> PaginatedUsersResponse:
        try:
            users_collection = DocumentTools.get_collection_unsafe(
                configuration=self.configuration,
                couchdb=self.couchdb,
                target_database_key='Users'
            )

            query_selector = selector if selector else {}

            logger.info(f"Query selector: {query_selector}")

            skip = (page - 1) * page_size

            count_result = users_collection.find(
                selector=query_selector,
                fields=['_id'],
                limit=MAX_FETCH_LIMIT
            )

            count_docs = count_result.get('docs', [])
            total = len(count_docs)
            logger.info(f"Count query returned {total} documents")

            result = users_collection.find(
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

            users = [self.build_response(doc) for doc in docs]

            logger.info(f"Returning {len(users)} users out of {total} total")

            return PaginatedUsersResponse(
                data=users,
                page=page,
                per_page=page_size,
                total=total,
                total_pages=total_pages,
                has_more=has_more
            )

        except Exception as e:
            logger.error(f"Error querying users: {str(e)}")
            import traceback
            logger.error(f"Traceback: {traceback.format_exc()}")
            raise HTTPException(
                status_code=http_status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"Database query failed: {str(e)}"
            )

    # ==================================================
    # Shared cases

    def find_shared_cases(self, user_id: str, other_user_id: str) -> List[str]:
        cases_collection = DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        )

        query = {
            "$or": [
                {
                    "clients": {
                        "$elemMatch": {
                            "$eq": user_id
                        }
                    }
                },
                {
                    "workers": {
                        "$elemMatch": {
                            "$eq": user_id
                        }
                    }
                }
            ]
        }

        result = cases_collection.find(query)
        user_cases = result.get('docs', [])

        shared_cases = []
        for case in user_cases:
            other_is_client = other_user_id in case.get('clients', [])
            other_is_worker = other_user_id in case.get('workers', [])

            if other_is_client or other_is_worker:
                shared_cases.append(case['_id'])

        return shared_cases

    def check_user_access(self, target_user_id: str) -> bool:
        # user should always be able to access their own data
        if self.current_user.id == target_user_id:
            return True

        # admins should always be able to access all data
        if self.current_user.is_admin:
            return True

        # users can access each other if they share a case together
        shared_cases = self.find_shared_cases(self.current_user.id, target_user_id)
        return len(shared_cases) > 0

    # ==================================================
    # Additional properties

    def save_additional_property(self, user_id: str, property_name: str, property_value: Any):
        user_doc = self.get_document(user_id=user_id)

        if 'additional_properties' not in user_doc:
            user_doc['additional_properties'] = {}

        user_doc['additional_properties'][property_name] = property_value
        user_doc['last_updated_at'] = datetime.utcnow().isoformat()
        user_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Users'
        ).save(user_doc)

        return user_doc

    def delete_additional_property(self, user_id: str, property_name: str):
        user_doc = self.get_document(user_id=user_id)

        if 'additional_properties' not in user_doc:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Additional properties not found"
            )

        if property_name not in user_doc['additional_properties']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail=f"Property '{property_name}' not found"
            )

        del user_doc['additional_properties'][property_name]

        user_doc['last_updated_at'] = datetime.utcnow().isoformat()
        user_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Users'
        ).save(user_doc)
