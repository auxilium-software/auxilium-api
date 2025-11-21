from http import HTTPStatus
from typing import Optional, Any
import yaml
from fastapi import HTTPException
from starlette import status

from common.utilities.case_validation_utilities import CaseValidationUtilities


class ConfigurationUtilities:
    def __init__(self, path: str):
        with open(path, "r") as file:
            self.config_data = yaml.safe_load(file)

            validator = CaseValidationUtilities()

            if not validator.validate_dict(self.config_data):
                error_msg = "Configuration file validation failure:\n" + "\n".join(validator.errors)
                raise HTTPException(
                    status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                    detail=error_msg
                )

    @staticmethod
    def get_default_value(*path: str) -> Any:
        defaults = {
            "Databases": {
                "MariaDB": {
                    "Port": 3306,
                },
                "CouchDB": {
                    "Port": 5984,
                },
                "Redis": {
                    "Port": 6379,
                    "ConnectTimeout": 5,
                    "SocketTimeout": 5,
                    "DecodeResponses": True,
                    "RetryOnTimeout": True,
                    "HealthCheckInterval": 30,
                },
                "RabbitMQ": {
                    "Port": 5672,
                    "Heartbeat": 600,
                    "BlockedConnectionTimeout": 300,
                },
                "ClickHouse": {
                    'Ports': {
                        "HTTP": 8123,
                        "TCP": 9000,
                    },
                    "Secure": False,
                    "Verify": True,
                    "Compression": True,
                    "ConnectTimeout": 10,
                    "SendReceiveTimeout": 300,
                    "BatchSize": 100,
                    "FlushInterval": 5,
                    "MaxQueueSize": 10000,
                    "TTLDays": 0,
                    "PartitionBy": "toYYYYMM(timestamp)",
                },
            },
            "ReCAPTCHA": {
                "ScoreThreshold": 0.5
            },
            "JWT": {
                "Algorithm": "HS256"
            },
        }

        temp = defaults
        try:
            for key in path:
                temp = temp[key]
            return temp
        except (KeyError, TypeError):
            raise KeyError(f"No default value found for path: {' -> '.join(path)}")

    def get_object(self, *path: str) -> Any:
        temp = self.config_data
        try:
            for key in path:
                temp = temp[key]
            return temp
        except (KeyError, TypeError):
            try:
                return self.get_default_value(*path)
            except KeyError:
                raise KeyError(
                    f"Configuration value not found for path: {' -> '.join(path)} (not in config file or defaults)")

    def get_string(self, *path: str) -> str:
        value = self.get_object(*path)
        if value is None:
            raise ValueError(
                f"Configuration value at path {' -> '.join(path)} is None and cannot be converted to string")
        return str(value)

    def get_int(self, *path: str) -> int:
        value = self.get_object(*path)
        if value is None:
            raise ValueError(f"Configuration value at path {' -> '.join(path)} is None and cannot be converted to int")
        try:
            return int(value)
        except (ValueError, TypeError) as e:
            raise ValueError(
                f"Configuration value at path {' -> '.join(path)} cannot be converted to int: {value}") from e

    def get_float(self, *path: str) -> float:
        value = self.get_object(*path)
        if value is None:
            raise ValueError(
                f"Configuration value at path {' -> '.join(path)} is None and cannot be converted to float")
        try:
            return float(value)
        except (ValueError, TypeError) as e:
            raise ValueError(
                f"Configuration value at path {' -> '.join(path)} cannot be converted to float: {value}") from e

    def get_bool(self, *path: str) -> bool:
        value = self.get_object(*path)
        if value is None:
            raise ValueError(f"Configuration value at path {' -> '.join(path)} is None and cannot be converted to bool")

        if isinstance(value, bool):
            return value

        if isinstance(value, str):
            lower_val = value.lower()
            if lower_val in ('true', '1', 'yes', 'on'):
                return True
            elif lower_val in ('false', '0', 'no', 'off'):
                return False
            else:
                raise ValueError(
                    f"Configuration value at path {' -> '.join(path)} cannot be converted to bool: '{value}'")

        return bool(value)


_configuration: Optional[ConfigurationUtilities] = None


def load_configuration(path: str) -> None:
    global _configuration
    _configuration = ConfigurationUtilities(path)


def get_configuration() -> ConfigurationUtilities:
    if _configuration is None:
        raise RuntimeError("Configuration not loaded. Call load_configuration() first.")
    return _configuration
