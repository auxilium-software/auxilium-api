using System.Security.Cryptography;
using System.Text;

namespace AuxiliumAPI.Common.Utilities
{
    public class HashingUtilities
    {
        public static string SHA256Hash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
        public static string SHA256Hash(byte[] data)
        {
            var hash = SHA256.HashData(data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
