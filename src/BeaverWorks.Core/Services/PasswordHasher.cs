using System.Security.Cryptography;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Hashes and verifies passwords using PBKDF2 (<see cref="Rfc2898DeriveBytes"/>),
/// the platform-standard mechanism required by the PRD Access Control section
/// (no custom crypto). Plaintext passwords never leave this boundary.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static (string Hash, string Salt, int Iterations) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSizeBytes);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), Iterations);
    }

    public static bool Verify(string password, string hash, string salt, int iterations)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expectedHash = Convert.FromBase64String(hash);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, iterations, Algorithm, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
