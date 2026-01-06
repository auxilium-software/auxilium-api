using AuxiliumAPI.Common.Enumerators;
using System.Security.Cryptography;
using System.Text;

namespace AuxiliumAPI.Common.Utilities
{
    /// <summary>
    /// Utilities related to generating and parsing UUIDs.
    /// </summary>
    public static class UUIDUtilities
    {
        /**
         * Takes in a DatabaseObjectType and generates a Version 5 UUID
         */
        private static readonly Dictionary<DatabaseObjectType, string> NamespacePaths = new()
        {
            [DatabaseObjectType.RefreshToken]           = "/auxilium/3/database_object/refresh_token",
            
            [DatabaseObjectType.User]                   = "/auxilium/3/database_object/mariadb/user",
            [DatabaseObjectType.UserAdditionalProperty] = "/auxilium/3/database_object/mariadb/user/additional_property",

            [DatabaseObjectType.Case]                   = "/auxilium/3/database_object/mariadb/case",
            [DatabaseObjectType.CaseTimelineItem]       = "/auxilium/3/database_object/mariadb/case/timeline_item",
            [DatabaseObjectType.CaseTodoItem]           = "/auxilium/3/database_object/mariadb/case/todo_item",
            [DatabaseObjectType.CaseAdditionalProperty] = "/auxilium/3/database_object/mariadb/case/additional_property",
            [DatabaseObjectType.CaseWorker]             = "/auxilium/3/database_object/mariadb/case/worker",
            [DatabaseObjectType.CaseClient]             = "/auxilium/3/database_object/mariadb/case/client",

            [DatabaseObjectType.File]                   = "/auxilium/3/database_object/mariadb/file",
            
            [DatabaseObjectType.Message]                = "/auxilium/3/database_object/mariadb/message",
            [DatabaseObjectType.MessageReadBy]          = "/auxilium/3/database_object/mariadb/message/read_by",
        };


        /**
         * Takes in a DatabaseObjectType and gets the precomputed Namespace UUID for it.
         */
        private static readonly Dictionary<DatabaseObjectType, Guid> NamespaceUuids =
            NamespacePaths.ToDictionary(
                kvp => kvp.Key,
                kvp => PathToUuid(kvp.Value)
            );

        /**
         * Generates a Version 5 UUID for the given DatabaseObjectType.
         */
        public static Guid GenerateV5(DatabaseObjectType objectType)
        {
            var namespaceId = NamespaceUuids[objectType];
            var name = $"{objectType}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}";

            return GenerateV5(namespaceId, name);
        }

        /**
         * Generates a Version 5 UUID given a namespace UUID and a name.
         */
        public static Guid GenerateV5(Guid namespaceId, string name)
        {
            var namespaceBytes = namespaceId.ToByteArray();
            var nameBytes = Encoding.UTF8.GetBytes(name);

            SwapByteOrder(namespaceBytes);

            var hash = SHA1.HashData(namespaceBytes.Concat(nameBytes).ToArray());

            var newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            newGuid[6] = (byte)((newGuid[6] & 0x0F) | 0x50);
            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

            SwapByteOrder(newGuid);

            return new Guid(newGuid);
        }

        /**
         * Converts a path string to a Version 5 UUID using SHA-1 hashing.
         */
        private static Guid PathToUuid(string path)
        {
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(path));

            var uuid = new byte[16];
            Array.Copy(hash, 0, uuid, 0, 16);

            uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50);
            uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80);

            return new Guid(uuid);
        }

        /**
         * Takes in a GUID byte array and swaps the byte order to match RFC 4122.
         */
        private static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        /**
         * Swaps two bytes in a byte array.
         */
        private static void SwapBytes(byte[] guid, int left, int right)
        {
            (guid[left], guid[right]) = (guid[right], guid[left]);
        }
    }
}
