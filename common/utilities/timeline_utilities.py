from common.uuid_handling import UUIDHandling
from enumerators.database_object_type import DatabaseObjectType
from enumerators.timeline_object_original_type import TimelineObjectOriginalType


def build_timeline_object(originalType: TimelineObjectOriginalType, originalData: dict) -> tuple[str, dict]:
    id = UUIDHandling.v5s(object_type=DatabaseObjectType.TIMELINE_ITEM)

    builder = {
        "originalType": originalType.value,
        "originalData": originalData
    }


    return id, builder
