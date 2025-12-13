using AuxiliumAPI.Common.Services.Interfaces;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace AuxiliumAPI.Common.Services;

public class PasswordService : IPasswordService
{
    private const int Argon2MemoryCost = 65536;
    private const int Argon2TimeCost = 3;
    private const int Argon2Parallelism = 1;
    private const int Argon2HashLength = 32;
    private const int Argon2SaltLength = 16;

    public string HashPassword(string password)
    {
        return HashPasswordArgon2(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        try
        {
            if (passwordHash.StartsWith("$argon2", StringComparison.Ordinal))
            {
                return VerifyPasswordArgon2(password, passwordHash);
            }
            else if (passwordHash.StartsWith("$2a$") || passwordHash.StartsWith("$2b$") || passwordHash.StartsWith("$2y$"))
            {
                return VerifyPasswordBCrypt(password, passwordHash);
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private string HashPasswordArgon2(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(Argon2SaltLength);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Argon2Parallelism,
            MemorySize = Argon2MemoryCost,
            Iterations = Argon2TimeCost
        };

        var hash = argon2.GetBytes(Argon2HashLength);

        var saltBase64 = Convert.ToBase64String(salt).TrimEnd('=');
        var hashBase64 = Convert.ToBase64String(hash).TrimEnd('=');

        return $"$argon2id$v=19$m={Argon2MemoryCost},t={Argon2TimeCost},p={Argon2Parallelism}${saltBase64}${hashBase64}";
    }

    private bool VerifyPasswordArgon2(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('$');

            if (parts.Length != 6)
            {
                return false;
            }

            var algorithm = parts[1];
            if (algorithm != "argon2id" && algorithm != "argon2i" && algorithm != "argon2d")
            {
                return false;
            }

            var paramParts = parts[3].Split(',');
            if (paramParts.Length != 3)
            {
                return false;
            }

            var memoryCost = int.Parse(paramParts[0].Split('=')[1]);
            var timeCost = int.Parse(paramParts[1].Split('=')[1]);
            var parallelism = int.Parse(paramParts[2].Split('=')[1]);

            var salt = Convert.FromBase64String(AddBase64Padding(parts[4]));
            var expectedHash = Convert.FromBase64String(AddBase64Padding(parts[5]));

            using var argon2 = algorithm switch
            {
                "argon2id" => (Argon2)new Argon2id(Encoding.UTF8.GetBytes(password)),
                "argon2i" => new Argon2i(Encoding.UTF8.GetBytes(password)),
                "argon2d" => new Argon2d(Encoding.UTF8.GetBytes(password)),
                _ => throw new ArgumentException($"Unknown Argon2 variant: {algorithm}")
            };

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = parallelism;
            argon2.MemorySize = memoryCost;
            argon2.Iterations = timeCost;

            var computedHash = argon2.GetBytes(expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    private static string AddBase64Padding(string base64)
    {
        var padding = (4 - base64.Length % 4) % 4;
        return base64 + new string('=', padding);
    }

    private bool VerifyPasswordBCrypt(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}
