import logging
from typing import Optional, Dict, Any
from datetime import datetime

from fastapi import HTTPException
from fastapi import status as http_status

from common.couchdb_document_structures.case_document import CaseDocument
from common.couchdb_document_structures.sub_structures.case_todo_object import CaseTodoObject
from common.document_modification.document_tools import DocumentTools
from common.document_modification.document_tools_interface import DocumentToolsInterface
from common.parameters import MAX_FETCH_LIMIT
from models.cases.case_response_model import CaseResponseModel
from models.cases.paginated_cases_response_model import PaginatedCasesResponse

logger = logging.getLogger(__name__)


class CaseDocumentTools(DocumentToolsInterface):
    def __init__(self, configuration, couchdb, current_user):
        self.configuration = configuration
        self.couchdb = couchdb
        self.current_user = current_user


    def save_document(self, document_builder: CaseDocument):
        collection = DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        )
        collection.save(document_builder.to_json())


    def get_document(self, case_id: str) -> dict:
        doc = DocumentTools.get_document_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases',
            document_id=case_id
        )

        is_client   = self.current_user.id in doc.get('clients', [])
        is_worker   = self.current_user.id in doc.get('workers', [])
        is_admin    = self.current_user.is_admin

        if not (is_client or is_worker or is_admin):
            raise HTTPException(
                status_code=http_status.HTTP_403_FORBIDDEN,
                detail="You don't have access to this case"
            )

        return doc



    def save_multiple_properties(self, case_id: str, properties: dict[str, Any]):
        case_doc = self.get_document(
            case_id=case_id
        )

        for property_key, property_value in properties.items():
            case_doc[property_key] = property_value

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()



    def build_response(self, doc: Dict) -> CaseResponseModel:
        return CaseResponseModel(
            id                      = doc['_id'],
            sensitivity             = doc['sensitivity'],
            title                   = doc['title'],
            status                  = doc['status'],
            brief_description       = doc['brief_description'],
            case_referrer           = doc['case_referrer'],
            description             = doc['description'],
            workers                 = doc['workers'],
            clients                 = doc['clients'],
            additional_properties   = doc['additional_properties'],
            todos                   = doc['todos'],
            timeline                = doc['timeline'],
            messages                = doc['messages'],
            files                   = doc['files'],
        )


    # ====================================================================================================
    # ADDITIONAL FUNCTIONS
    # ====================================================================================================


    async def get_cases_with_filter(
            self,
            selector: Dict,
            page: int,
            page_size: int,
            search: Optional[str],
            status: Optional[str],
            priority: Optional[str],
            sort: str,
            order: str,
    ) -> PaginatedCasesResponse:
        try:
            database = DocumentTools.get_collection_unsafe(
                configuration=self.configuration,
                couchdb=self.couchdb,
                target_database_key='Cases'
            )

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

            cases = [CaseDocumentTools.build_response(doc) for doc in docs]

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


    # ==================================================
    # additional properties


    def get_additional_properties(self, case_id: str) -> dict:
        case_doc = self.get_document(
            case_id=case_id
        )

        return case_doc['additional_properties']


    def save_additional_property(self, case_id: str, property_name: str, property_value: Any):
        case_doc = self.get_document(
            case_id=case_id
        )

        case_doc['additional_properties'][property_name] = property_value

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
        return case_doc


    def delete_additional_property(self, case_id: str, property_name: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        del case_doc['additional_properties'][property_name]

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)



    # ==================================================
    # clients // case workers


    def add_client(self, case_id: str, user_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if user_id not in case_doc['clients']:
            case_doc['clients'].append(user_id)

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
        return case_doc


    def add_case_worker(self, case_id: str, user_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if user_id not in case_doc['workers']:
            case_doc['workers'].append(user_id)

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
        return case_doc


    def remove_client(self, case_id: str, user_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if user_id not in case_doc['clients']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Client not found in this case"
            )

        case_doc["clients"].remove(user_id)

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
        return case_doc


    def remove_case_worker(self, case_id: str, user_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if user_id not in case_doc['workers']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Case Worker not found in this case"
            )

        case_doc["workers"].remove(user_id)

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
        return case_doc


    # ==================================================
    # todos

    def add_todo(self, case_id: str, todo_object: CaseTodoObject) -> None:
        case_doc = self.get_document(
            case_id=case_id
        )

        case_doc['todos'].append(
            todo_object.to_json()
        )

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)

    def get_todo(self, case_id: str, todo_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if todo_id not in case_doc['todos']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Todo object not found."
            )

        return case_doc['todos'][todo_id]

    def update_todo_properties(self, case_id: str, todo_id: str, properties: dict[str, Any]):
        case_doc = self.get_document(
            case_id=case_id
        )

        if todo_id not in case_doc['todos']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Todo object not found."
            )

        for property_name, property_value in properties.items():
            case_doc['todos'][todo_id][property_name] = property_value

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id
        case_doc['todos'][todo_id]['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['todos'][todo_id]['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)

        return case_doc['todos'][todo_id]

    def delete_todo(self, case_id: str, todo_id: str):
        case_doc = self.get_document(
            case_id=case_id
        )

        if todo_id not in case_doc['todos']:
            raise HTTPException(
                status_code=http_status.HTTP_404_NOT_FOUND,
                detail="Todo object not found."
            )

        del case_doc['todos'][todo_id]

        case_doc['last_updated_at'] = datetime.utcnow().isoformat()
        case_doc['last_updated_by'] = self.current_user.id

        DocumentTools.get_collection_unsafe(
            configuration=self.configuration,
            couchdb=self.couchdb,
            target_database_key='Cases'
        ).save(case_doc)
