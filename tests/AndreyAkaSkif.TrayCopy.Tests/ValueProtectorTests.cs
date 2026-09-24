using System.Security.Cryptography;

namespace AndreyAkaSkif.TrayCopy.Tests;

public class ValueProtectorTests
{
    private const string Value = "ghp_secret-token";

    [Fact]
    public void Protect_ShouldReturnValueAsIs_WhenModeIsNone()
    {
        // Act
        var stored = ValueProtector.Protect(Value, ProtectionMode.None);

        // Assert
        Assert.Equal(Value, stored);
    }

    [Fact]
    public void Unprotect_ShouldReturnStoredAsIs_WhenModeIsNone()
    {
        // Act
        var value = ValueProtector.Unprotect(Value, ProtectionMode.None);

        // Assert
        Assert.Equal(Value, value);
    }

    [Fact]
    public void Protect_ShouldReturnBase64WithoutValue_WhenModeIsDpapi()
    {
        // Act
        var stored = ValueProtector.Protect(Value, ProtectionMode.Dpapi);

        // Assert
        Assert.DoesNotContain(Value, stored, StringComparison.Ordinal);
        Assert.NotEmpty(Convert.FromBase64String(stored));
    }

    [Theory]
    [InlineData("ghp_secret-token")]
    [InlineData("токен с пробелами и кириллицей")]
    [InlineData("")]
    public void Unprotect_ShouldRestoreValue_WhenModeIsDpapi(string value)
    {
        // Arrange
        var stored = ValueProtector.Protect(value, ProtectionMode.Dpapi);

        // Act
        var restored = ValueProtector.Unprotect(stored, ProtectionMode.Dpapi);

        // Assert
        Assert.Equal(value, restored);
    }

    [Fact]
    public void Unprotect_ShouldThrowFormatException_WhenStoredIsNotBase64()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ValueProtector.Unprotect(Value, ProtectionMode.Dpapi));
    }

    [Fact]
    public void Unprotect_ShouldThrowCryptographicException_WhenStoredIsNotDpapiData()
    {
        // Arrange
        var stored = Convert.ToBase64String("not dpapi data"u8);

        // Act & Assert
        Assert.ThrowsAny<CryptographicException>(
            () => ValueProtector.Unprotect(stored, ProtectionMode.Dpapi));
    }
}
