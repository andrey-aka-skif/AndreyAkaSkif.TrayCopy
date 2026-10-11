namespace AndreyAkaSkif.TrayCopy.Tests;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("0.2.0", 0, 2, 0, null)]
    [InlineData("10.20.30", 10, 20, 30, null)]
    [InlineData("0.2.1-dev.5", 0, 2, 1, "dev.5")]
    [InlineData("0.0.0-local", 0, 0, 0, "local")]
    [InlineData("1.0.0-rc-1.x-y", 1, 0, 0, "rc-1.x-y")]
    public void TryParse_ShouldAcceptSemVerWithoutBuildMetadata(
        string value, int major, int minor, int patch, string? prerelease)
    {
        // Act
        var parsed = ReleaseVersion.TryParse(value, out var version);

        // Assert
        Assert.True(parsed);
        Assert.Equal(major, version!.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(patch, version.Patch);
        Assert.Equal(prerelease, version.Prerelease);
        Assert.Equal(value, version.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0.2")]
    [InlineData("0.2.0.1")]
    [InlineData("v0.2.0")]
    [InlineData("0.2.0-")]
    [InlineData("0.2.0-dev..5")]
    [InlineData("0.2.0-dev.05")]
    [InlineData("0.2.0-dev_5")]
    [InlineData("0.2.0+6f1d992")]
    [InlineData("01.2.0")]
    [InlineData("-1.2.0")]
    [InlineData(" 0.2.0")]
    [InlineData("99999999999.0.0")]
    public void TryParse_ShouldRejectOtherFormats(string value)
    {
        // Act
        var parsed = ReleaseVersion.TryParse(value, out var version);

        // Assert
        Assert.False(parsed);
        Assert.Null(version);
    }

    [Fact]
    public void Parse_ShouldThrowFormatException_WhenFormatIsWrong()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ReleaseVersion.Parse("1.0"));
    }

    // Пары по возрастанию: левая младше правой. Порядок — из примеров спецификации SemVer и
    // версий, которые получают сборки проекта
    [Theory]
    [InlineData("0.2.0", "0.2.1")]
    [InlineData("0.2.9", "0.3.0")]
    [InlineData("0.9.0", "1.0.0")]
    [InlineData("0.2.0", "0.10.0")]
    [InlineData("0.2.1-dev.5", "0.2.1")]
    [InlineData("0.2.0", "0.2.1-dev.5")]
    [InlineData("0.0.0-local", "0.2.0")]
    [InlineData("0.2.1-dev.5", "0.2.1-dev.10")]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
    [InlineData("1.0.0-alpha.beta", "1.0.0-beta")]
    [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    public void CompareTo_ShouldOrderBySemVerPrecedence(string lower, string higher)
    {
        // Arrange
        var left = ReleaseVersion.Parse(lower);
        var right = ReleaseVersion.Parse(higher);

        // Assert
        Assert.True(left < right);
        Assert.True(right > left);
        Assert.True(left.CompareTo(right) < 0);
        Assert.True(right.CompareTo(left) > 0);
        Assert.NotEqual(left, right);
    }

    [Theory]
    [InlineData("0.2.0")]
    [InlineData("0.2.1-dev.5")]
    public void CompareTo_ShouldBeZero_ForSameVersion(string value)
    {
        // Arrange
        var left = ReleaseVersion.Parse(value);
        var right = ReleaseVersion.Parse(value);

        // Assert
        Assert.Equal(0, left.CompareTo(right));
        Assert.True(left <= right);
        Assert.True(left >= right);
        Assert.Equal(left, right);
    }

    [Fact]
    public void CompareTo_ShouldPlaceNullBelowAnyVersion()
    {
        // Arrange
        var version = ReleaseVersion.Parse("0.0.0-local");

        // Assert
        Assert.True(version.CompareTo(null) > 0);
        Assert.True(version > null);
        Assert.True(null < version);
    }
}
