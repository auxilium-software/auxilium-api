import os

import couchdb

from common.utilities.configuration import CONFIGURATION

COUCHDB_CONNECTION: couchdb.Server


def get_couchdb_path():
    protocol = CONFIGURATION.get_string('Databases', 'CouchDB', 'Protocol')
    hostname = CONFIGURATION.get_string('Databases', 'CouchDB', 'Host')
    port     = CONFIGURATION.get_int('Databases', 'CouchDB', 'Port')
    username = CONFIGURATION.get_string('Databases', 'CouchDB', 'Username')
    password = CONFIGURATION.get_string('Databases', 'CouchDB', 'Password')

    return f"{protocol}://{username}:{password}@{hostname}:{port}/"


def get_couchdb_connection():
    global COUCHDB_CONNECTION

    database_url = get_couchdb_path()

    COUCHDB_CONNECTION = couchdb.Server(database_url)

    for x in [
        "Cases",
        "Users",
    ]:
        try:
            db = COUCHDB_CONNECTION[CONFIGURATION.get_string('Databases', 'CouchDB', 'Databases', x)]
        except couchdb.ResourceNotFound:
            db = COUCHDB_CONNECTION.create(CONFIGURATION.get_string('Databases', 'CouchDB', 'Databases', x))

    try:
        yield COUCHDB_CONNECTION
    finally:
        COUCHDB_CONNECTION.close()
