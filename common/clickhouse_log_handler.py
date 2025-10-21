import logging
import json
from contextlib import contextmanager
from datetime import datetime
from typing import Generator, Optional, Dict, Any, List
from queue import Queue, Empty
import threading
import time
import socket

from clickhouse_driver import Client

from common.utilities.configuration import get_configuration, load_configuration

logger = logging.getLogger(__name__)


class ClickHouseLogHandler(logging.Handler):
    def __init__(self):
        super().__init__()

        self.client         = None
        self.queue          = None
        self.stop_event     = None
        self.client_lock    = None
        self.worker_thread  = None

        self.configuration  = get_configuration()

        self.database   = self.configuration.get_string('Databases', 'ClickHouse', 'Database')
        self.table  = self.configuration.get_string('Databases', 'ClickHouse', 'Table')

        self._setup_client()
        self._ensure_database_and_table()

        self.queue = Queue()
        self.stop_event = threading.Event()
        self.client_lock = threading.Lock()
        self.worker_thread = threading.Thread(target=self._worker, daemon=True)
        self.worker_thread.start()

        logger.info(f"ClickHouse log handler initialized for table {self.database}.{self.table}")

    def _setup_client(self):
        try:
            self.client = Client(
                host                    = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                port                    = self.configuration.get_int('Databases', 'ClickHouse', 'Port'),
                user                    = self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                password                = self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                secure                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Secure'),
                verify                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Verify'),
                compression             = self.configuration.get_bool('Databases', 'ClickHouse', 'Compression'),
                connect_timeout         = self.configuration.get_int('Databases', 'ClickHouse', 'ConnectTimeout'),
                send_receive_timeout    = self.configuration.get_int('Databases', 'ClickHouse', 'SendReceiveTimeout'),
                settings                = {
                    'use_numpy': False,
                },
            )

            result = self.client.execute('SELECT 1')
            logger.debug(f"ClickHouse connection test successful: {result}")

        except Exception as e:
            logger.error(f"Failed to setup ClickHouse client: {e}")
            raise

    def _ensure_database_and_table(self):
        max_retries = 3
        retry_delay = 1

        for attempt in range(max_retries):
            try:
                # test we can actually reach the server
                try:
                    self.client.execute('SELECT 1')
                except Exception as conn_error:
                    logger.error(f"Cannot connect to ClickHouse: {conn_error}")
                    raise

                # create the database if it doesn't already exist
                create_db_query = f"CREATE DATABASE IF NOT EXISTS {self.database}"
                logger.info(f"Creating database with: {create_db_query}")
                self.client.execute(create_db_query)
                logger.info(f"Database {self.database} created or already exists")

                # check the database exists
                check_db = f"SHOW DATABASES LIKE '{self.database}'"
                result = self.client.execute(check_db)
                if not result or self.database not in [r[0] for r in result]:
                    raise Exception(f"Database {self.database} was not created successfully")

                # reconnect with the database specified
                self.client.disconnect()
                self.client = Client(
                    host                    = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                    port                    = self.configuration.get_int('Databases', 'ClickHouse', 'Port'),
                    database                = self.configuration.get_string('Databases', 'ClickHouse', 'Database'),
                    user                    = self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                    password                = self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                    secure                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Secure'),
                    verify                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Verify'),
                    compression             = self.configuration.get_bool('Databases', 'ClickHouse', 'Compression'),
                    connect_timeout         = self.configuration.get_int('Databases', 'ClickHouse', 'ConnectTimeout'),
                    send_receive_timeout    = self.configuration.get_int('Databases', 'ClickHouse', 'SendReceiveTimeout'),
                    settings                = {
                        'use_numpy': False,
                    },
                )

                # test we can actually reach the server (again)
                self.client.execute('SELECT 1')

                # create the table with a TTL if it's set in config
                ttl_clause = f"TTL timestamp + INTERVAL {self.configuration.get_int('Databases', 'ClickHouse', 'TTLDays')} DAY" if self.configuration.get_int('Databases', 'ClickHouse', 'TTLDays') > 0 else ""

                create_table_query = f"""
                CREATE TABLE IF NOT EXISTS {self.table} (
                    timestamp DateTime64(3),
                    level String,
                    logger_name String,
                    message String,
                    module String,
                    function String,
                    line_number UInt32,
                    thread_name String,
                    thread_id UInt64,
                    process_id UInt32,
                    hostname String,
                    extra_data String,
                    exception_type String,
                    exception_message String,
                    exception_traceback String,
                    inserted_at DateTime DEFAULT now()
                ) ENGINE = MergeTree()
                PARTITION BY {self.configuration.get_string('Databases', 'ClickHouse', 'PartitionBy')}
                ORDER BY (timestamp, level, logger_name)
                {ttl_clause}
                """

                logger.info(f"Creating table {self.table} in database {self.database}")
                self.client.execute(create_table_query)

                # sanity check to make sure the table actually exists now
                check_table = f"SHOW TABLES LIKE '{self.table}'"
                result = self.client.execute(check_table)
                if not result or self.table not in [r[0] for r in result]:
                    raise Exception(f"Table {self.table} was not created successfully")

                logger.info(f"Table {self.database}.{self.table} created or already exists")
                return

            except Exception as e:
                logger.error(f"Attempt {attempt + 1}/{max_retries} failed: {e}")
                if attempt < max_retries - 1:
                    logger.info(f"Retrying in {retry_delay} seconds...")
                    time.sleep(retry_delay)
                    retry_delay *= 2
                    # reconnect without database for a retry
                    try:
                        self.client.disconnect()
                    except:
                        pass
                    self._setup_client()
                else:
                    logger.error("All attempts to create database and table failed")
                    raise

    def emit(self, record: logging.LogRecord):
        try:
            # check queue size to prevent memory hogging
            # TODO: don't actually drop the logs lol
            if self.queue.qsize() >= self.configuration.get_int('Databases', 'ClickHouse', 'MaxQueueSize'):
                logger.warning("ClickHouse log queue is full, dropping log entry")
                return

            log_entry = self._format_record(record)
            self.queue.put(log_entry)

        except Exception as e:
            self.handleError(record)

    def _format_record(self, record: logging.LogRecord) -> Dict[str, Any]:
        # extract exception info if it exists
        exception_type = ""
        exception_message = ""
        exception_traceback = ""
        if record.exc_info:
            exception_type = record.exc_info[0].__name__ if record.exc_info[0] else ""
            exception_message = str(record.exc_info[1]) if record.exc_info[1] else ""
            if record.exc_text:
                exception_traceback = record.exc_text
            else:
                import traceback
                exception_traceback = ''.join(traceback.format_exception(*record.exc_info))

        # extract extra fields
        extra_data = {}
        standard_fields = {
            'name',
            'msg',
            'args',
            'created',
            'filename',
            'funcName',
            'levelname',
            'levelno',
            'lineno',
            'module',
            'msecs',
            'pathname',
            'process',
            'processName',
            'relativeCreated',
            'thread',
            'threadName',
            'exc_info',
            'exc_text',
            'stack_info',
            'taskName',
        }

        for key, value in record.__dict__.items():
            if key not in standard_fields:
                try:
                    json.dumps(value)  # quick sanity check to make sure value is JSON serialisable
                    extra_data[key] = value
                except (TypeError, ValueError):
                    extra_data[key] = str(value)

        return {
            'timestamp':            datetime.fromtimestamp(record.created),
            'level':                record.levelname,
            'logger_name':          record.name,
            'message':              record.getMessage(),
            'module':               record.module or '',
            'function':             record.funcName or '',
            'line_number':          record.lineno,
            'thread_name':          record.threadName or '',
            'thread_id':            record.thread or 0,
            'process_id':           record.process or 0,
            'hostname':             socket.gethostname(),
            'extra_data':           json.dumps(extra_data) if extra_data else '',
            'exception_type':       exception_type,
            'exception_message':    exception_message,
            'exception_traceback':  exception_traceback
        }

    def _worker(self):
        batch = []
        last_flush = time.time()

        while not self.stop_event.is_set():
            try:
                timeout = min(1.0, self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'))
                log_entry = self.queue.get(timeout=timeout)
                batch.append(log_entry)

                current_time = time.time()
                if len(batch) >= self.configuration.get_int('Databases', 'ClickHouse', 'BatchSize') or current_time - last_flush >= self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'):
                    if batch:
                        self._send_batch(batch)
                        batch = []
                        last_flush = current_time

            except Empty:
                current_time = time.time()
                if batch and current_time - last_flush >= self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'):
                    self._send_batch(batch)
                    batch = []
                    last_flush = current_time
            except Exception as e:
                logger.error(f"Error in ClickHouse log worker: {e}")

        if batch:
            self._send_batch(batch)

    def _send_batch(self, batch: List[Dict[str, Any]]):
        if not batch:
            return

        with self.client_lock:  # thread safety
            try:
                if not self.client:
                    logger.warning("ClickHouse client is None, attempting to reconnect...")
                    self._setup_client()
                    if self.client:
                        self.client.disconnect()
                        self.client = Client(
                            host=self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                            port=self.configuration.get_int('Databases', 'ClickHouse', 'Port'),
                            database=self.database,
                            user=self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                            password=self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                            settings={'use_numpy': False}
                        )

                try:
                    self.client.execute('SELECT 1')
                except Exception as ping_error:
                    logger.warning(f"ClickHouse connection lost: {ping_error}, reconnecting...")
                    try:
                        self.client.disconnect()
                    except:
                        pass
                    self.client = Client(
                        host=self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                        port=self.configuration.get_int('Databases', 'ClickHouse', 'Port'),
                        database=self.database,
                        user=self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                        password=self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                        settings={'use_numpy': False}
                    )

                clean_batch = []
                for entry in batch:
                    # Remove inserted_at if it exists (it's auto-generated)
                    clean_entry = {k: v for k, v in entry.items() if k != 'inserted_at'}
                    clean_batch.append(clean_entry)

                columns = [
                    'timestamp',
                    'level',
                    'logger_name',
                    'message',
                    'module',
                    'function',
                    'line_number',
                    'thread_name',
                    'thread_id',
                    'process_id',
                    'hostname',
                    'extra_data',
                    'exception_type',
                    'exception_message',
                    'exception_traceback',
                ]

                query = f"""
                INSERT INTO {self.table} 
                ({', '.join(columns)}) 
                VALUES
                """

                self.client.execute(query, clean_batch)
                logger.debug(f"Successfully sent {len(batch)} log entries to ClickHouse")

            except Exception as e:
                logger.error(f"Failed to send logs to ClickHouse: {e}")
                if batch and len(batch) > 0:
                    logger.debug(f"Sample log entry keys: {list(batch[0].keys())}")
                try:
                    if self.client:
                        self.client.disconnect()
                except:
                    pass
                self.client = None

    def close(self):
        logger.debug("Closing ClickHouse log handler...")

        if self.stop_event is not None:
            self.stop_event.set()

        if self.worker_thread and self.worker_thread.is_alive():
            timeout = self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval') + 1
            self.worker_thread.join(timeout=timeout)

        if self.queue is not None:
            remaining = []
            while not self.queue.empty():
                try:
                    remaining.append(self.queue.get_nowait())
                except Empty:
                    break

            if remaining:
                logger.debug(f"Flushing {len(remaining)} remaining log entries...")
                self._send_batch(remaining)

        if self.client:
            try:
                self.client.disconnect()
                logger.debug("ClickHouse client disconnected")
            except Exception as e:
                logger.error(f"Error disconnecting ClickHouse client: {e}")
            finally:
                self.client = None

        super().close()
        logger.debug("ClickHouse log handler closed")


def initialize_clickhouse_database():
    try:
        configuration = get_configuration()

        host = configuration.get_string('Databases', 'ClickHouse', 'Host')
        port = configuration.get_int('Databases', 'ClickHouse', 'Port')
        database = configuration.get_string('Databases', 'ClickHouse', 'Database')
        username = configuration.get_string('Databases', 'ClickHouse', 'Username')
        password = configuration.get_string('Databases', 'ClickHouse', 'Password')

        logger.info(f"Initializing ClickHouse database {database} at {host}:{port}")

        client = Client(
            host=host,
            port=port,
            user=username,
            password=password,
            settings={'use_numpy': False}
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
