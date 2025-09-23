from typing import Optional, Any
import yaml


class Configuration:
    def __init__(self, path: str):
        with open(path, "r") as file:
            self.config_data = yaml.load(file, Loader=yaml.FullLoader)

    def get_object(self, *path: str, default: Any = None) -> Any:
        temp = self.config_data
        try:
            for key in path:
                temp = temp[key]
            return temp
        except (KeyError, TypeError):
            return default

    def get_string(self, *path: str, default: Optional[str] = None) -> Optional[str]:
        value = self.get_object(*path)
        return str(value) if value is not None else default

    def get_int(self, *path: str, default: Optional[int] = None) -> Optional[int]:
        value = self.get_object(*path)
        if value is None:
            return default
        try:
            return int(value)
        except (ValueError, TypeError):
            return default

    def get_float(self, *path: str, default: Optional[float] = None) -> Optional[float]:
        value = self.get_object(*path)
        if value is None:
            return default
        try:
            return float(value)
        except (ValueError, TypeError):
            return default

    def get_bool(self, *path: str, default: Optional[bool] = None) -> Optional[bool]:
        value = self.get_object(*path)
        if value is None:
            return default
        if isinstance(value, bool):
            return value
        if isinstance(value, str):
            lower_val = value.lower()
            if lower_val in ('true', '1', 'yes', 'on'):
                return True
            elif lower_val in ('false', '0', 'no', 'off'):
                return False
            else:
                return default
        return bool(value)


_configuration: Optional[Configuration] = None


def load_configuration(path: str) -> None:
    global _configuration
    _configuration = Configuration(path)


def get_configuration() -> Configuration:
    if _configuration is None:
        raise RuntimeError("Configuration not loaded. Call load_configuration() first.")
    return _configuration
