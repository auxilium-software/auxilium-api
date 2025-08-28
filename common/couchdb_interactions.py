import os

import couchdb
from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker


COUCHDB_CONNECTION: couchdb.Server


def get_couchdb_path():
    load_dotenv()

    username = os.getenv('COUCHDB_USERNAME')
    password = os.getenv('COUCHDB_PASSWORD')
    hostname = os.getenv('COUCHDB_HOSTNAME')
    port     = int(os.getenv('COUCHDB_PORT'))

    return f"http://{username}:{password}@{hostname}:{port}/"


def get_couchdb_connection():
    global COUCHDB_CONNECTION

    database_url = get_couchdb_path()

    COUCHDB_CONNECTION = couchdb.Server(database_url)
    try:
        yield COUCHDB_CONNECTION
    finally:
        COUCHDB_CONNECTION.close()


def build_couchdb_database():
    global COUCHDB_CONNECTION

    try:
        cases_db = COUCHDB_CONNECTION[os.getenv('COUCHDB_DATABASE_CASES')]
    except couchdb.ResourceNotFound:
        cases_db = COUCHDB_CONNECTION.create(os.getenv('COUCHDB_DATABASE_CASES'))

    try:
        users_db = COUCHDB_CONNECTION[os.getenv('COUCHDB_DATABASE_USERS')]
    except couchdb.ResourceNotFound:
        users_db = COUCHDB_CONNECTION.create(os.getenv('COUCHDB_DATABASE_USERS'))
