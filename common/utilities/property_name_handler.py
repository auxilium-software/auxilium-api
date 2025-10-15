import re
from datetime import datetime
from typing import Tuple, Dict, Any

import unicodedata


class PropertyNameHandler:
    @staticmethod
    def normalize_key(display_name: str) -> str:
        # Remove accents/special chars
        nfd = unicodedata.normalize('NFD', display_name)
        cleaned = ''.join(char for char in nfd if unicodedata.category(char) != 'Mn')

        # convert to snake_case

        # replace non-alphanumeric with underscores
        snake = re.sub(r'[^a-zA-Z0-9]+', '_', cleaned)
        # remove leading/trailing underscores
        snake = snake.strip('_')
        # lowercase
        snake = snake.lower()
        # collapse multiple underscores
        snake = re.sub(r'_+', '_', snake)

        return snake or 'untitled'

    @staticmethod
    def create_property_metadata(display_name: str, content: Any, content_type: str, user_id: str) -> Dict:
        return {
            'pretty_name': display_name,
            'url_slug': PropertyNameHandler.normalize_key(display_name),
            'content': content,
            'content_type': content_type,
            'created_at': datetime.utcnow().isoformat(),
            'created_by': user_id,
            'original_name': display_name,
        }

    @staticmethod
    def handle_property_name(raw_name: str) -> Tuple[str, str]:
        # check if it's already snake_case
        if raw_name == raw_name.lower() and not ' ' in raw_name:
            # already normalized, generate display name
            display_name = raw_name.replace('_', ' ').title()
            return raw_name, display_name
        else:
            # pretty name provided, normalize for storage
            storage_key = PropertyNameHandler.normalize_key(raw_name)
            return storage_key, raw_name
