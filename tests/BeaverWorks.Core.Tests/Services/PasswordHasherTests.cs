using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentSaltsAndHashes()
    {
        var first = PasswordHasher.Hash("correct-horse-battery-staple");
        var second = PasswordHasher.Hash("correct-horse-battery-staple");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var (hash, salt, iterations) = PasswordHasher.Hash("correct-horse-battery-staple");

        Assert.True(PasswordHasher.Verify("correct-horse-battery-staple", hash, salt, iterations));
    }

    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        var (hash, salt, iterations) = PasswordHasher.Hash("correct-horse-battery-staple");

        Assert.False(PasswordHasher.Verify("wrong-password", hash, salt, iterations));
    }

    [Fact]
    public void Verify_MalformedStoredData_ReturnsFalseWithoutThrowing()
    {
        var result = PasswordHasher.Verify("any-password", "not-base64!!", "also-not-base64!!", 100_000);

        Assert.False(result);
    }
}
