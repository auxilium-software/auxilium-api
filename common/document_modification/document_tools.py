
from fastapi import HTTPException
from fastapi import status

class DocumentTools:
    @staticmethod
    def get_collection_unsafe(configuration, couchdb, target_database_key: str):
        db_name = configuration.get_string('Databases', 'CouchDB', 'Databases', target_database_key)
        return couchdb[db_name]

    @staticmethod
    def get_document_unsafe(configuration, couchdb, target_database_key: str, document_id: str) -> dict:
        cases_db = couchdb[configuration.get_string('Databases', 'CouchDB', 'Databases', target_database_key)]
        case_doc = cases_db.get(document_id)

        if not case_doc:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="Document not found"
            )

        return case_doc
