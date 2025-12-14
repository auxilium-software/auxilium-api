using AuxiliumAPI.Common.Enumerators;
using System.Security.Cryptography;
using System.Text;

namespace AuxiliumAPI.Common.Utilities
{
    public static class UUIDUtilities
    {
        private static readonly Dictionary<DatabaseObjectType, string> NamespacePaths = new()
        {
            [DatabaseObjectType.User]               = "/auxilium/3/database_object/couchdb/user",
            [DatabaseObjectType.Case]               = "/auxilium/3/database_object/couchdb/case",
            [DatabaseObjectType.CaseTimelineItem]   = "/auxilium/3/database_object/couchdb/case/timeline_item",
            [DatabaseObjectType.CaseTodoItem]       = "/auxilium/3/database_object/couchdb/case/todo_item",
            [DatabaseObjectType.File]               = "/auxilium/3/database_object/couchdb/file",
            [DatabaseObjectType.Message]            = "/auxilium/3/database_object/couchdb/message",
        };

        private static readonly Dictionary<DatabaseObjectType, Guid> NamespaceUuids =
            NamespacePaths.ToDictionary(
                kvp => kvp.Key,
                kvp => PathToUuid(kvp.Value)
            );

        public static Guid GenerateV5(DatabaseObjectType objectType)
        {
            var namespaceId = NamespaceUuids[objectType];
            var name = $"{objectType}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}";

            return GenerateV5(namespaceId, name);
        }

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

        private static Guid PathToUuid(string path)
        {
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(path));

            var uuid = new byte[16];
            Array.Copy(hash, 0, uuid, 0, 16);

            uuid[6] = (byte)((uuid[6] & 0x0F) | 0x50);
            uuid[8] = (byte)((uuid[8] & 0x3F) | 0x80);

            return new Guid(uuid);
        }

        private static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            (guid[left], guid[right]) = (guid[right], guid[left]);
        }
    }
}
