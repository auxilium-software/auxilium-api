from typing import Optional

import yaml


class Configuration:
    def __init__(self, path: str):
        with open(path, "r") as file:
            self.config_data = yaml.load(file, Loader=yaml.FullLoader)

    def get_object(self, *path: str, default=None) -> object:
        temp = self.config_data
        for key in path:
            temp = temp[key]
        return temp

    def get_string(self, *path: str, default: str = None) -> str:
        return str(self.get_object(*path, default=default))

    def get_int(self, *path: str, default: int = None) -> int:
        return int(self.get_string(*path, default=default))

    def get_float(self, *path: str, default: float = None) -> float:
        return float(self.get_string(*path, default=default))

    def get_bool(self, *path: str, default: bool = None) -> bool:
        return bool(self.get_string(*path, default=default))


_configuration: Optional[Configuration] = None


def load_configuration(path: str) -> None:
    global _configuration
    _configuration = Configuration(path=path)


def get_configuration() -> Configuration:
    if _configuration is None:
        raise RuntimeError("Configuration not loaded. Call load_configuration() first.")
    return _configuration
