using BeaverWorks.Core.Models;
using BeaverWorks.Core.Persistence;

namespace BeaverWorks.Core.Tests.Persistence;

public class CredentialStoreTests : IDisposable
{
    private readonly string _tempFilePath;

    public CredentialStoreTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"beaverworks-credentials-tests-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public void LoadAll_MissingFile_ReturnsEmptySet()
    {
        var store = new CredentialStore(_tempFilePath);

        var result = store.LoadAll();

        Assert.Empty(result);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_RoundTripsCredentials()
    {
        var store = new CredentialStore(_tempFilePath);
        var credentials = new[]
        {
            new UserCredential { Username = "anna", PasswordHash = "hash", Salt = "salt", Iterations = 100_000 },
        };

        store.SaveAll(credentials);
        var result = store.LoadAll();

        var loaded = Assert.Single(result);
        Assert.Equal("anna", loaded.Username);
        Assert.Equal("hash", loaded.PasswordHash);
        Assert.Equal("salt", loaded.Salt);
        Assert.Equal(100_000, loaded.Iterations);
    }
}
