import json
import logging
from contextlib import contextmanager
from typing import Generator, Any, Optional, Union

import redis

from common.utilities.configuration import get_configuration

logger = logging.getLogger(__name__)


@contextmanager
def get_redis_connection() -> Generator[redis.Redis, None, None]:
    connection = None
    try:
        configuration = get_configuration()

        connection = redis.Redis(
            host                    = configuration.get_string('Databases', 'Redis', 'Host'),
            port                    = configuration.get_int('Databases', 'Redis', 'Port'),
            password                = configuration.get_string('Databases', 'Redis', 'Password'),
            db                      = configuration.get_int('Databases', 'Redis', 'Database'),
            decode_responses        = configuration.get_bool('Databases', 'Redis', 'DecodeResponses'),
            socket_connect_timeout  = configuration.get_int('Databases', 'Redis', 'ConnectTimeout'),
            socket_timeout          = configuration.get_int('Databases', 'Redis', 'SocketTimeout'),
            retry_on_timeout        = configuration.get_bool('Databases', 'Redis', 'RetryOnTimeout'),
            health_check_interval   = configuration.get_int('Databases', 'Redis', 'HealthCheckInterval'),
        )

        connection.ping()
        logger.info("Redis connection established")
        yield connection

    except Exception as e:
        logger.error(f"Failed to connect to Redis: {e}")
        raise
    finally:
        if connection:
            try:
                connection.close()
                logger.info("Redis connection closed")
            except Exception as e:
                logger.error(f"Error closing Redis connection: {e}")


def get_redis_dependency():
    with get_redis_connection() as connection:
        yield connection
