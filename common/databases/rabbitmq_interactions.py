import json

import pika
from contextlib import contextmanager
from typing import Generator
import logging

from common.utilities.configuration import get_configuration

logger = logging.getLogger(__name__)


@contextmanager
def get_rabbitmq_connection() -> Generator[pika.BlockingConnection, None, None]:
    connection = None
    try:
        configuration = get_configuration()

        credentials = pika.PlainCredentials(
            username    = configuration.get_string('Databases', 'RabbitMQ', 'Username'),
            password    = configuration.get_string('Databases', 'RabbitMQ', 'Password')
        )
        parameters = pika.ConnectionParameters(
            host                        = configuration.get_string('Databases', 'RabbitMQ', 'Host'),
            port                        = configuration.get_int('Databases', 'RabbitMQ', 'Port'),
            virtual_host                = configuration.get_string('Databases', 'RabbitMQ', 'VirtualHost'),
            credentials                 = credentials,
            heartbeat                   = configuration.get_int('Databases', 'RabbitMQ', 'Heartbeat'),
            blocked_connection_timeout  = configuration.get_int('Databases', 'RabbitMQ', 'BlockedConnectionTimeout'),
        )

        connection = pika.BlockingConnection(parameters)
        logger.info("RabbitMQ connection established")
        yield connection

    except Exception as e:
        logger.error(f"Failed to connect to RabbitMQ: {e}")
        raise
    finally:
        if connection and not connection.is_closed:
            try:
                connection.close()
                logger.info("RabbitMQ connection closed")
            except Exception as e:
                logger.error(f"Error closing RabbitMQ connection: {e}")


def get_rabbitmq_dependency():
    with get_rabbitmq_connection() as connection:
        yield connection


def publish_message(connection: pika.BlockingConnection, queue_key: str, message: dict):
    try:
        configuration = get_configuration()

        channel = connection.channel()

        channel.queue_declare(queue=configuration.get_string('Databases', 'RabbitMQ', 'Queues', queue_key), durable=True)

        channel.basic_publish(
            exchange=configuration.get_string('Databases', 'RabbitMQ', 'Exchange'),
            routing_key=configuration.get_string('Databases', 'RabbitMQ', 'Queues', queue_key),
            body=json.dumps(message),
            properties=pika.BasicProperties(
                delivery_mode=2,  # make the message persistent
            )
        )

        logger.info(f"Message published to queue '{queue_key}': {message}")
        channel.close()

    except Exception as e:
        logger.error(f"Failed to publish message to '{queue_key}': {e}")
        raise

