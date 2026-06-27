using System.Security.Cryptography;
using OnTime.Application.Interfaces;

namespace OnTime.Infrastructure.Security;

/// <summary>
/// PBKDF2-SHA256, 16-byte salt. Hash format: iterations.base64(salt).base64(hash) — the
/// iteration count is embedded so it can be raised later without invalidating existing
/// passwords (old hashes keep verifying against whatever count they were created with).
/// Legacy 2-part hashes (no embedded count, from before this format) fall back to 10,000.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    // OWASP 2023 guidance for PBKDF2-SHA256 is ~600k iterations. New hashes use this; the
    // value is embedded per-hash so raising it again later is also non-breaking.
    private const int CurrentIterations = 600_000;
    private const int LegacyIterations = 10_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, CurrentIterations, Algorithm, HashBytes);
        return $"{CurrentIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string storedHash, string password)
    {
        var parts = storedHash.Split('.');

        try
        {
            string saltPart, hashPart;
            int iterations;

            if (parts.Length == 3)
            {
                iterations = int.Parse(parts[0]);
                saltPart = parts[1];
                hashPart = parts[2];
            }
            else if (parts.Length == 2)
            {
                iterations = LegacyIterations;
                saltPart = parts[0];
                hashPart = parts[1];
            }
            else
            {
                return false;
            }

            var salt = Convert.FromBase64String(saltPart);
            var expectedHash = Convert.FromBase64String(hashPart);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, HashBytes);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }
}
