using Shouldly;
using OnTime.Infrastructure.Security;

namespace OnTime.Tests.Unit;

/// <summary>
/// Pure unit tests (no DB/HTTP) — the hash format embeds its iteration count so raising
/// CurrentIterations later never invalidates passwords hashed under the old count.
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_RoundTrips()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Teste123!");

        hasher.Verify(hash, "Teste123!").ShouldBeTrue();
        hasher.Verify(hash, "WrongPassword").ShouldBeFalse();
    }

    [Fact]
    public void Verify_AcceptsLegacyTwoPartHashFormat_AtTenThousandIterations()
    {
        // Simulates a password hashed before the iteration count was embedded in the string.
        const int legacyIterations = 10_000;
        var salt = new byte[16];
        Random.Shared.NextBytes(salt);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "Teste123!", salt, legacyIterations, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        var legacyStoredHash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        var hasher = new Pbkdf2PasswordHasher();
        hasher.Verify(legacyStoredHash, "Teste123!").ShouldBeTrue();
        hasher.Verify(legacyStoredHash, "WrongPassword").ShouldBeFalse();
    }

    [Fact]
    public void Hash_EmbedsCurrentIterationCount_AsFirstSegment()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("Teste123!");

        var parts = hash.Split('.');
        parts.Length.ShouldBe(3);
        int.Parse(parts[0]).ShouldBeGreaterThanOrEqualTo(600_000);
    }
}
