import hashlib
from datetime import datetime
from pathlib import Path
from typing import Any

from fastapi import HTTPException
from fastapi import status as http_status

from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType

def get_message_details(message_id: str, mariadb, couchdb, config):
    couchdb_data = couchdb[config.get_string('Databases', 'CouchDB', 'Databases', 'Messages')].get(message_id)
    return None, couchdb_data
