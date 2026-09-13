using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;

namespace BeaverWorks.Core.Services;

public enum RegisterOutcome
{
    Success,
    DuplicateUsername,
}

public readonly record struct RegisterResult(RegisterOutcome Outcome)
{
    public bool Succeeded => Outcome == RegisterOutcome.Success;
}

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
}

public readonly record struct LoginResult(LoginOutcome Outcome)
{
    public bool Succeeded => Outcome == LoginOutcome.Success;
}

/// <summary>
/// Single entry point for registering a new local account and attempting a
/// login. Enforces case-insensitive username matching, rejects duplicate
/// usernames on registration, and never distinguishes "user not found" from
/// "wrong password" on login failure.
/// </summary>
public sealed class AuthService
{
    private readonly ICredentialStore _credentialStore;

    public AuthService(ICredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public RegisterResult Register(string username, string password)
    {
        var credentials = _credentialStore.LoadAll();

        var duplicate = credentials.Any(c => string.Equals(c.Username, username, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            return new RegisterResult(RegisterOutcome.DuplicateUsername);
        }

        var (hash, salt, iterations) = PasswordHasher.Hash(password);
        var newCredential = new UserCredential
        {
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            Iterations = iterations,
        };

        _credentialStore.SaveAll([.. credentials, newCredential]);

        return new RegisterResult(RegisterOutcome.Success);
    }

    public LoginResult Login(string username, string password)
    {
        var credentials = _credentialStore.LoadAll();

        var match = credentials.FirstOrDefault(c => string.Equals(c.Username, username, StringComparison.OrdinalIgnoreCase));
        if (match is null || !PasswordHasher.Verify(password, match.PasswordHash, match.Salt, match.Iterations))
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        return new LoginResult(LoginOutcome.Success);
    }
}
