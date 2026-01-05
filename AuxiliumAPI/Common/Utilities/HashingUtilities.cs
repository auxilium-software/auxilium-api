using System.Security.Cryptography;
using System.Text;

namespace AuxiliumAPI.Common.Utilities
{
    /// <summary>
    /// Utilities related to hashing operations.
    /// </summary>
    public class HashingUtilities
    {
        /**
         * Hashes a given string into SHA256 and returns the hex representation.
         */
        public static string SHA256Hash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /**
         * Hashes a given byte array into SHA256 and returns the hex representation.
         */
        public static string SHA256Hash(byte[] data)
        {
            var hash = SHA256.HashData(data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
