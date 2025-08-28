import os

from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker


MARIADB_CONNECTION = None


def get_mariadb_path():
    load_dotenv()

    username = os.getenv('MARIADB_USERNAME')
    password = os.getenv('MARIADB_PASSWORD')
    hostname = os.getenv('MARIADB_HOSTNAME')
    port = os.getenv('MARIADB_PORT')
    database = os.getenv('MARIADB_DATABASE')

    return f"mysql+pymysql://{username}:{password}@{hostname}/{database}"


def get_mariadb_connection():
    global MARIADB_CONNECTION

    database_url = get_mariadb_path()

    engine = create_engine(database_url)
    session = sessionmaker(autocommit=False, autoflush=False, bind=engine)

    MARIADB_CONNECTION = session()
    try:
        yield MARIADB_CONNECTION
    finally:
        MARIADB_CONNECTION.close()
