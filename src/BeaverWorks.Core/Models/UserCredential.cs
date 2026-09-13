namespace BeaverWorks.Core.Models;

/// <summary>
/// Represents one local account's stored identity: the username plus the
/// PBKDF2 hash/salt/iteration count needed to verify a login attempt later.
/// The plaintext password is never stored.
/// </summary>
public sealed class UserCredential
{
    public required string Username { get; init; }

    public required string PasswordHash { get; init; }

    public required string Salt { get; init; }

    public required int Iterations { get; init; }
}
