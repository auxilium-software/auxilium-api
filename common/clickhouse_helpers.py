import logging
from typing import Optional

from clickhouse_driver import Client

from common.clickhouse_log_handler import logger, ClickHouseLogHandler
from common.utilities.configuration_utilities import get_configuration


def initialize_clickhouse_database():
    try:
        configuration = get_configuration()

        # Get connection parameters
        host = configuration.get_string('Databases', 'ClickHouse', 'Host')
        port = configuration.get_int('Databases', 'ClickHouse', 'Ports', 'TCP')
        database = configuration.get_string('Databases', 'ClickHouse', 'Database')

        logger.info(f"Initializing ClickHouse database {database} at {host}:{port}")

        client = Client(
            host        = host,
            port        = port,
            user        = configuration.get_string('Databases', 'ClickHouse', 'Username'),
            password    = configuration.get_string('Databases', 'ClickHouse', 'Password'),
            settings    = {
                'use_numpy': False,
            },
        )

        try:
            create_db = f"CREATE DATABASE IF NOT EXISTS {database}"
            client.execute(create_db)
            logger.info(f"ClickHouse database {database} initialized successfully")

            result = client.execute(f"SHOW DATABASES LIKE '{database}'")
            if result and database in [r[0] for r in result]:
                logger.info(f"Verified database {database} exists")
                return True
            else:
                logger.error(f"Database {database} creation verification failed")
                return False

        finally:
            client.disconnect()

    except Exception as e:
        logger.error(f"Failed to initialize ClickHouse database: {e}")
        return False


def setup_clickhouse_logging(
        logger_name: Optional[str] = None,
        level: int = logging.INFO,
        include_console: bool = True,
) -> logging.Logger:
    target_logger = logging.getLogger(logger_name)
    target_logger.setLevel(level)

    for handler in target_logger.handlers:
        if isinstance(handler, ClickHouseLogHandler):
            logger.info("ClickHouse handler already configured")
            return target_logger

    try:
        if not initialize_clickhouse_database():
            logger.warning("Failed to initialize ClickHouse database, continuing without ClickHouse logging")
        else:
            ch_handler = ClickHouseLogHandler()
            ch_handler.setLevel(level)
            target_logger.addHandler(ch_handler)

    except Exception as e:
        logger.error(f"failed to set up ClickHouse logging: {e}")
        raise e

    if include_console:
        console_handler = logging.StreamHandler()
        console_handler.setLevel(level)

        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
        console_handler.setFormatter(formatter)
        target_logger.addHandler(console_handler)

    return target_logger
