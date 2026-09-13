using BeaverWorks.Core.Persistence;
using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"beaverworks-auth-tests-{Guid.NewGuid()}.json");
        _authService = new AuthService(new CredentialStore(_tempFilePath));
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void Register_NewUsername_Succeeds()
    {
        var result = _authService.Register("anna", "password123");

        Assert.True(result.Succeeded);
        Assert.Equal(RegisterOutcome.Success, result.Outcome);
    }

    [Fact]
    public void Register_DuplicateUsername_FailsWithDuplicateOutcome()
    {
        _authService.Register("anna", "password123");

        var result = _authService.Register("anna", "different-password");

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterOutcome.DuplicateUsername, result.Outcome);
    }

    [Fact]
    public void Register_DuplicateUsernameDifferentCase_FailsWithDuplicateOutcome()
    {
        _authService.Register("anna", "password123");

        var result = _authService.Register("Anna", "different-password");

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterOutcome.DuplicateUsername, result.Outcome);
    }

    [Fact]
    public void Login_CorrectCredentials_Succeeds()
    {
        _authService.Register("anna", "password123");

        var result = _authService.Login("anna", "password123");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Login_WrongPassword_FailsWithGenericInvalidCredentials()
    {
        _authService.Register("anna", "password123");

        var result = _authService.Login("anna", "wrong-password");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public void Login_UnknownUsername_FailsWithGenericInvalidCredentials()
    {
        var result = _authService.Login("nobody", "password123");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public void Login_DifferentUsernameCasing_Succeeds()
    {
        _authService.Register("anna", "password123");

        var result = _authService.Login("Anna", "password123");

        Assert.True(result.Succeeded);
    }
}
