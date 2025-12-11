import hashlib
from datetime import datetime
from pathlib import Path
from typing import Any

from fastapi import HTTPException
from fastapi import status as http_status

from common.couchdb_document_structures.file_document import FileDocument
from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType


def get_file_details(file_id: str, mariadb, couchdb, config):
    couchdb_data = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Files')].get(file_id)
    return None, couchdb_data


def get_file_contents(file_id: str, config):
    file_path = Path(config.get_string("FileSystem", "RootStorageDirectories", "AuxLFS")) / f"{file_id}.bin"

    if not file_path.exists():
        raise HTTPException(
            status_code=http_status.HTTP_404_NOT_FOUND,
            detail="File not found"
        )

    with open(file_path, "rb") as f:
        hex_data = f.read()

    if isinstance(hex_data, bytes):
        hex_string = hex_data.decode('utf-8')
    else:
        hex_string = hex_data

    binary_data = bytes.fromhex(hex_string)

    return binary_data


def create_file(
        document_type: str,
        document_id: str,
        file_name: str,
        file_type: Any,
        uploaded_by: str,
        file_contents: Any,  # should be bytes
        description: str,

        couchdb,
        config
):
    main_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', document_type)]
    files_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', "Files")]
    main_doc = main_db.get(document_id)

    if not main_doc:
        raise HTTPException(
            status_code=http_status.HTTP_404_NOT_FOUND,
            detail="Document does not exist"
        )

    file_id = UUIDHandling.v5s(object_type=DatabaseObjectType.FILE)

    if isinstance(file_contents, str):
        file_bytes = file_contents.encode('utf-8')
    else:
        file_bytes = file_contents

    hash_object = hashlib.sha1()
    hash_object.update(file_bytes)
    file_hash = hash_object.hexdigest()

    file_doc_builder = FileDocument()
    file_doc_builder.set_required_properties(
        _id=file_id,
        created_by=uploaded_by,
        filename=file_name,
        description=description,
        content_type=file_type,
        hash=file_hash,
        size=len(file_bytes),
    )

    main_doc['files'].append(f"auxlfs://localhost/{file_id}?size={len(file_bytes)}&hash={file_hash}")
    main_doc['last_updated_at'] = datetime.utcnow().isoformat()

    # Create directory and write binary file
    storage_path = Path(config.get_string("FileSystem", "RootStorageDirectories", "AuxLFS"))
    storage_path.mkdir(parents=True, exist_ok=True)

    file_path = storage_path / f"{file_id}.bin"
    with open(file_path, "wb") as f:  # Write in binary mode
        f.write(file_bytes)

    main_db.save(main_doc)
    files_db.save(file_doc_builder.to_json())
    return main_doc


def delete_file_from_lfs(
        document_type: str,
        document_id: str,
        file_id: str,

        couchdb,
        config
) -> bool:
    main_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', document_type)]
    files_db = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', "Files")]

    main_doc = main_db.get(document_id)
    successful_doc_deletion = files_db.delete(files_db.get(file_id))

    # Delete the physical file
    storage_path = Path(config.get_string("FileSystem", "RootStorageDirectories", "AuxLFS"))
    file_path = storage_path / f"{file_id}.bin"

    if file_path.exists():
        file_path.unlink()

    return successful_doc_deletion
