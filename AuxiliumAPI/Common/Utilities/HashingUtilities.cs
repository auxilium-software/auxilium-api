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
    }
}
