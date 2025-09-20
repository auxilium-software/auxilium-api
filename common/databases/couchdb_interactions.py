import logging
import os
from typing import Generator

import couchdb3

from common.utilities.case_utilities import get_cases_collection
from common.utilities.configuration import get_configuration


COUCHDB_CONNECTION: couchdb3.Server


def get_couchdb_path():
    configuration = get_configuration()

    protocol = configuration.get_string('Databases', 'CouchDB', 'Protocol')
    hostname = configuration.get_string('Databases', 'CouchDB', 'Host')
    port     = configuration.get_int('Databases', 'CouchDB', 'Port')
    username = configuration.get_string('Databases', 'CouchDB', 'Username')
    password = configuration.get_string('Databases', 'CouchDB', 'Password')

    return f"{protocol}://{username}:{password}@{hostname}:{port}/"


def get_couchdb_connection() -> Generator[couchdb3.Server, None, None]:
    global COUCHDB_CONNECTION
    configuration = get_configuration()
    database_url = get_couchdb_path()
    COUCHDB_CONNECTION = couchdb3.Server(database_url)

    for x in ["Cases", "Users"]:
        db_name = configuration.get_string('Databases', 'CouchDB', 'Databases', x)
        try:
            db = COUCHDB_CONNECTION[db_name]
        except couchdb3.ResourceNotFound:
            db = COUCHDB_CONNECTION.create(db_name)

    try:
        yield COUCHDB_CONNECTION
    finally:
        pass
