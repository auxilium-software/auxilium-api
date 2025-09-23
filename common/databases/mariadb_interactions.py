import logging
from contextlib import contextmanager
from typing import Generator

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, Session

from common.utilities.configuration import get_configuration

logger = logging.getLogger(__name__)


def get_mariadb_path():
    configuration = get_configuration()

    hostname = configuration.get_string('Databases', 'MariaDB', 'Host')
    port     = configuration.get_int('Databases', 'MariaDB', 'Port')
    username = configuration.get_string('Databases', 'MariaDB', 'Username')
    password = configuration.get_string('Databases', 'MariaDB', 'Password')
    database = configuration.get_string('Databases', 'MariaDB', 'Database')

    return f"mysql+pymysql://{username}:{password}@{hostname}:{port}/{database}"


@contextmanager
def get_mariadb_connection() -> Generator[Session, None, None]:
    session = None
    try:
        database_url = get_mariadb_path()

        engine = create_engine(database_url)
        session_maker = sessionmaker(autocommit=False, autoflush=False, bind=engine)
        session = session_maker()

        logger.info("MariaDB connection established")
        yield session

    except Exception as e:
        logger.error(f"Failed to connect to MariaDB: {e}")
        if session:
            session.rollback()
        raise
    finally:
        if session:
            try:
                session.close()
                logger.info("MariaDB connection closed")
            except Exception as e:
                logger.error(f"Error closing MariaDB connection: {e}")


def get_mariadb_dependency():
    with get_mariadb_connection() as session:
        yield session
