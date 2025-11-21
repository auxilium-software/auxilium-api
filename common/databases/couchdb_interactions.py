import logging
from contextlib import contextmanager
from typing import Generator

import couchdb3

from common.utilities.configuration_utilities import get_configuration

logger = logging.getLogger(__name__)


def get_couchdb_path():
    configuration = get_configuration()

    protocol = configuration.get_string('Databases', 'CouchDB', 'Protocol')
    hostname = configuration.get_string('Databases', 'CouchDB', 'Host')
    port     = configuration.get_int('Databases', 'CouchDB', 'Port')
    username = configuration.get_string('Databases', 'CouchDB', 'Username')
    password = configuration.get_string('Databases', 'CouchDB', 'Password')

    return f"{protocol}://{username}:{password}@{hostname}:{port}/"


@contextmanager
def get_couchdb_connection() -> Generator[couchdb3.Server, None, None]:
    connection = None
    try:
        configuration = get_configuration()
        database_url = get_couchdb_path()
        connection = couchdb3.Server(database_url)

        # make sure required databases exist
        for db_type in ["Cases", "Files", "Messages", "Users"]:
            db_name = configuration.get_string('Databases', 'CouchDB', 'Databases', db_type)
            try:
                db = connection[db_name]
            except couchdb3.exceptions.NotFoundError:
                db = connection.create(db_name)
                logger.info(f"Created CouchDB database: {db_name}")

        logger.info("CouchDB connection established")
        yield connection

    except Exception as e:
        logger.error(f"Failed to connect to CouchDB: {e}")
        raise
    finally:
        if connection:
            try:
                logger.info("CouchDB connection released")
            except Exception as e:
                logger.error(f"Error releasing CouchDB connection: {e}")


def get_couchdb_dependency():
    with get_couchdb_connection() as connection:
        yield connection
