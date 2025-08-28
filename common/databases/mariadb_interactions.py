from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

from common.utilities.configuration import CONFIGURATION

MARIADB_CONNECTION = None


def get_mariadb_path():
    hostname = CONFIGURATION.get_string('Databases', 'MariaDB', 'Host')
    port     = CONFIGURATION.get_int('Databases', 'MariaDB', 'Port')
    username = CONFIGURATION.get_string('Databases', 'MariaDB', 'Username')
    password = CONFIGURATION.get_string('Databases', 'MariaDB', 'Password')
    database = CONFIGURATION.get_string('Databases', 'MariaDB', 'Database')


    return f"mysql+pymysql://{username}:{password}@{hostname}:{port}/{database}"


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
