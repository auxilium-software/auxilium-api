import yaml
from typing import Optional

class Configuration:
    def __init__(self, path: str):
        with open(path, "r") as file:
            self.config_data = yaml.load(file, Loader=yaml.FullLoader)

    def get_object(self, *path: str) -> object:
        temp = self.config_data
        for key in path:
            temp = temp[key]
        return temp

    def get_string(self, *path: str) -> str:
        return str(self.get_object(*path))

    def get_int(self, *path: str) -> int:
        return int(self.get_string(*path))

    def get_float(self, *path: str) -> float:
        return float(self.get_string(*path))

_configuration: Optional[Configuration] = None

def load_configuration(path: str) -> None:
    global _configuration
    _configuration = Configuration(path=path)

def get_configuration() -> Configuration:
    if _configuration is None:
        raise RuntimeError("Configuration not loaded. Call load_configuration() first.")
    return _configuration
