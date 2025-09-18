import json
import logging
from datetime import datetime
from typing import Optional, Any

from fastapi import HTTPException, Depends, status, APIRouter, Path, Body
from fastapi.responses import Response, JSONResponse

from common.databases.couchdb_interactions import get_couchdb_connection
from common.utilities.configuration import get_configuration
from common.utilities.property_name_handler import PropertyNameHandler
from common.utilities.security_utilities import (
    get_current_user
)
from common.utilities.user_utilities import check_user_access, get_user_properties, save_user_property
from enumerators.property_type import PropertyType

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/api/v3/cases/{case_id:path}", tags=["Cases"])
