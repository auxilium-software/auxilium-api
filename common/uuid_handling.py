import os
import uuid
from uuid import UUID

from dotenv import load_dotenv

from common.utilities.configuration import get_configuration
from enumerators.database_object_type import DatabaseObjectType


class UUIDHandling:
    @staticmethod
    def v5(ObjectType: DatabaseObjectType) -> UUID:
        configuration = get_configuration()

        qualified_dns = configuration.get_string('Instance', 'QualifiedDNS')

        global_namespace    = uuid.uuid5(uuid.NAMESPACE_DNS, qualified_dns)
        object_namespace    = uuid.uuid5(global_namespace, ObjectType.value)
        new_object_id       = uuid.uuid5(object_namespace, str(uuid.uuid4()))

        return new_object_id

    @staticmethod
    def v5s(ObjectType: DatabaseObjectType) -> str:
        return str(UUIDHandling.v5(ObjectType))
