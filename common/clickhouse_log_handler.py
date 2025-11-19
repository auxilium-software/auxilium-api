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

        self.client = None
        self.queue = None
        self.stop_event = None
        self.client_lock = None
        self.worker_thread = None

        self.configuration = get_configuration()

        self.database = self.configuration.get_string('Databases', 'ClickHouse', 'Database')
        self.table = self.configuration.get_string('Databases', 'ClickHouse', 'Table')

        self._setup_client()

        self._ensure_database_and_table()

        self.queue = Queue()
        self.stop_event = threading.Event()
        self.client_lock = threading.Lock()
        self.worker_thread = threading.Thread(target=self._worker, daemon=True)
        self.worker_thread.start()

        logger.info(f"ClickHouse log handler initialized for table {self.database}.{self.table}")


    @staticmethod
    def _format_record(record: logging.LogRecord) -> Dict[str, Any]:
        # extract exception information if this log includes an exception
        exception_type = ""
        exception_message = ""
        exception_traceback = ""
        if record.exc_info:
            exception_type = record.exc_info[0].__name__ if record.exc_info[0] else ""
            exception_message = str(record.exc_info[1]) if record.exc_info[1] else ""
            # if there's a pre-formatted exception text, use that
            if record.exc_text:
                exception_traceback = record.exc_text
            # if not, use the full traceback
            else:
                import traceback
                exception_traceback = ''.join(traceback.format_exception(*record.exc_info))

        # standard LogRecord attributes to exclude
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

        # if data doesn't match the above list of "standard fields", add it to this dict instead
        extra_data = {}

        # grab any non-standard fields and chuck them into `extra_data`
        for key, value in record.__dict__.items():
            if key not in standard_fields:
                # check that the data is able to be serialised as JSON
                try:
                    json.dumps(value)
                    extra_data[key] = value
                # it's not, so just convert it to a string
                except (TypeError, ValueError):
                    extra_data[key] = str(value)

        # format the data into a dict for ClickHouse to store
        return {
            'timestamp': datetime.fromtimestamp(record.created),
            'level': record.levelname,
            'logger_name': record.name,
            'message': record.getMessage(),  # formatted message with args applied
            'module': record.module or '',
            'function': record.funcName or '',
            'line_number': record.lineno,
            'thread_name': record.threadName or '',
            'thread_id': record.thread or 0,
            'process_id': record.process or 0,
            'hostname': socket.gethostname(),
            'extra_data': json.dumps(extra_data) if extra_data else '',
            'exception_type': exception_type,
            'exception_message': exception_message,
            'exception_traceback': exception_traceback
        }

    def _setup_client(self):
        try:
            self.client = Client(
                host                    = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                port                    = self.configuration.get_int('Databases', 'ClickHouse', 'Ports', 'TCP'),
                user                    = self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                password                = self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                secure                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Secure'),
                verify                  = self.configuration.get_bool('Databases', 'ClickHouse', 'Verify'),
                compression             = self.configuration.get_bool('Databases', 'ClickHouse', 'Compression'),
                connect_timeout         = self.configuration.get_int('Databases', 'ClickHouse', 'ConnectTimeout'),
                send_receive_timeout    = self.configuration.get_int('Databases', 'ClickHouse', 'SendReceiveTimeout'),
                settings                = {
                    'use_numpy': False,  # we disable numpy so we can use simpler data types
                },
            )

            # test the connection with a simple query
            result = self.client.execute('SELECT 1')
            logger.debug(f"ClickHouse connection test successful: {result}")

        except Exception as e:
            logger.error(f"Failed to setup ClickHouse client: {e}")
            raise

    def _ensure_database_and_table(self):
        max_retries = 3
        retry_delay = 1  # initial delay in seconds

        for attempt in range(max_retries):
            try:
                # test the connection to the server
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

                # make sure the database was created successfully
                check_db = f"SHOW DATABASES LIKE '{self.database}'"
                result = self.client.execute(check_db)
                if not result or self.database not in [r[0] for r in result]:
                    raise Exception(f"Database {self.database} was not created successfully")

                # reconnect to the server now specifying the database
                self.client.disconnect()
                self.client = Client(
                    host                    = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                    port                    = self.configuration.get_int('Databases', 'ClickHouse', 'Ports', 'TCP'),
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

                # test the connection to the server again
                self.client.execute('SELECT 1')

                # build the ttl clause for automatic data deletion
                ttl_clause = f"TTL timestamp + INTERVAL {self.configuration.get_int('Databases', 'ClickHouse', 'TTLDays')} DAY"\
                    if self.configuration.get_int('Databases', 'ClickHouse', 'TTLDays') > 0\
                    else ""

                # create the log table within the database
                create_table_query = f"""
                CREATE TABLE IF NOT EXISTS {self.table} (
                    timestamp DateTime64(3),              -- Timestamp with millisecond precision
                    level String,                         -- Log level (DEBUG, INFO, WARNING, ERROR, CRITICAL)
                    logger_name String,                   -- Name of the logger that created this record
                    message String,                       -- The formatted log message
                    module String,                        -- Python module name
                    function String,                      -- Function name where log was called
                    line_number UInt32,                   -- Line number in source file
                    thread_name String,                   -- Name of the thread
                    thread_id UInt64,                     -- Thread identifier
                    process_id UInt32,                    -- Process identifier
                    hostname String,                      -- Machine hostname
                    extra_data String,                    -- Additional structured data as JSON
                    exception_type String,                -- Exception class name if present
                    exception_message String,             -- Exception message if present
                    exception_traceback String,           -- Full exception traceback if present
                    inserted_at DateTime DEFAULT now()    -- When the record was inserted into ClickHouse
                ) ENGINE = MergeTree()
                PARTITION BY {self.configuration.get_string('Databases', 'ClickHouse', 'PartitionBy')}
                ORDER BY (timestamp, level, logger_name)  -- Sorting key for efficient queries
                {ttl_clause}
                """

                logger.info(f"Creating table {self.table} in database {self.database}")
                self.client.execute(create_table_query)

                # check that the table was created successfully
                check_table = f"SHOW TABLES LIKE '{self.table}'"
                result = self.client.execute(check_table)
                if not result or self.table not in [r[0] for r in result]:
                    raise Exception(f"Table {self.table} was not created successfully")

                logger.info(f"Table {self.database}.{self.table} created or already exists")

                # awesome, everything seems to work, let's exit the retry loop
                return

            except Exception as e:
                logger.error(f"Attempt {attempt + 1}/{max_retries} failed: {e}")
                if attempt < max_retries - 1:
                    # still have retries left, let's wait then try again
                    logger.info(f"Retrying in {retry_delay} seconds...")
                    time.sleep(retry_delay)
                    retry_delay *= 2  # exponential backoff

                    # reconnect without database for a clean retry
                    try:
                        self.client.disconnect()
                    except:
                        pass
                    self._setup_client()
                else:
                    # all retries have failed
                    logger.error("All attempts to create database and table failed")
                    raise

    def emit(self, record: logging.LogRecord):
        try:
            # check the queue size to avoid using too much memory
            # TODO: need better handling here instead of just dropping the logs
            if self.queue.qsize() >= self.configuration.get_int('Databases', 'ClickHouse', 'MaxQueueSize'):
                logger.warning("ClickHouse log queue is full, dropping log entry")
                return

            # format the log record into a dictionary for ClickHouse
            log_entry = self._format_record(record)

            # Add to queue for asynchronous batch processing
            self.queue.put(log_entry)

        except Exception as e:
            # Call the base class error handler if formatting/queueing fails
            self.handleError(record)


    def _worker(self):
        batch = []  # current batch of log entries
        last_flush = time.time()  # track when we last sent a batch

        # run until `stop_event` is set (during shutdown)
        while not self.stop_event.is_set():
            try:
                # wait for a log entry from the queue (with timeout)
                timeout = min(1.0, self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'))
                log_entry = self.queue.get(timeout=timeout)
                batch.append(log_entry)

                current_time = time.time()
                # send batch if it's full or enough time has passed
                if len(batch) >= self.configuration.get_int('Databases', 'ClickHouse', 'BatchSize') or current_time - last_flush >= self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'):
                    if batch:
                        self._send_batch(batch)
                        batch = []
                        last_flush = current_time

            except Empty:
                # the queue is empty, check if we should flush the current batch
                current_time = time.time()
                if batch and current_time - last_flush >= self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval'):
                    self._send_batch(batch)
                    batch = []
                    last_flush = current_time
            except Exception as e:
                logger.error(f"Error in ClickHouse log worker: {e}")

        # final flush on shutdown if there are remaining entries
        if batch:
            self._send_batch(batch)

    def _send_batch(self, batch: List[Dict[str, Any]]):
        if not batch:
            return

        # use lock to ensure only one thread accesses the client at a time
        with self.client_lock:
            try:
                # check if client exists, reconnect if needed
                if not self.client:
                    logger.warning("ClickHouse client is None, attempting to reconnect...")
                    self._setup_client()
                    if self.client:
                        self.client.disconnect()
                        self.client = Client(
                            host        = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                            port        = self.configuration.get_int('Databases', 'ClickHouse', 'Ports', 'TCP'),
                            database    = self.database,
                            user        = self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                            password    = self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                            settings    = {
                                'use_numpy': False,
                            },
                        )

                # test the connection with a simple query
                try:
                    self.client.execute('SELECT 1')
                # connection has been lost, so we should attempt a reconnect
                except Exception as ping_error:
                    logger.warning(f"ClickHouse connection lost: {ping_error}, reconnecting...")
                    try:
                        self.client.disconnect()
                    except:
                        pass
                    self.client = Client(
                        host        = self.configuration.get_string('Databases', 'ClickHouse', 'Host'),
                        port        = self.configuration.get_int('Databases', 'ClickHouse', 'Ports', 'TCP'),
                        database    = self.database,
                        user        = self.configuration.get_string('Databases', 'ClickHouse', 'Username'),
                        password    = self.configuration.get_string('Databases', 'ClickHouse', 'Password'),
                        settings    = {
                            'use_numpy': False,
                        },
                    )

                # clean the batch by removing auto-generated fields
                clean_batch = []
                for entry in batch:
                    # remove `inserted_at` since it has a DEFAULT value in the table
                    clean_entry = {k: v for k, v in entry.items() if k != 'inserted_at'}
                    clean_batch.append(clean_entry)

                # define the column order for the insertion
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

                # build and execute the INSERT query
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
                    # log sample data for debugging
                    logger.debug(f"Sample log entry keys: {list(batch[0].keys())}")

                # clean up the failed connection
                try:
                    if self.client:
                        self.client.disconnect()
                except:
                    pass
                self.client = None

    def close(self):
        logger.debug("Closing ClickHouse log handler...")

        # signal the worker thread to stop
        if self.stop_event is not None:
            self.stop_event.set()

        # wait for worker thread to finish processing
        if self.worker_thread and self.worker_thread.is_alive():
            timeout = self.configuration.get_int('Databases', 'ClickHouse', 'FlushInterval') + 1
            self.worker_thread.join(timeout=timeout)

        # flush any remaining entries in the queue
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

        # disconnect the ClickHouse client
        if self.client:
            try:
                self.client.disconnect()
                logger.debug("ClickHouse client disconnected")
            except Exception as e:
                logger.error(f"Error disconnecting ClickHouse client: {e}")
            finally:
                self.client = None

        # call parent class close method
        super().close()
        logger.debug("ClickHouse log handler closed")
