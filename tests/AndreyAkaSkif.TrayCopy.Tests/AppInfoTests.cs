namespace AndreyAkaSkif.TrayCopy.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("0.1.0+6f1d992b7eaeaf8f45c04e05c58f47dfa188761c", "0.1.0")]
    [InlineData("0.1.1-dev.2+6f1d992b7eaeaf8f45c04e05c58f47dfa188761c", "0.1.1-dev.2")]
    [InlineData("0.0.0-local", "0.0.0-local")]
    public void TrimSourceRevision_ShouldDropCommitId(string informationalVersion, string expected)
    {
        // Act
        var version = AppInfo.TrimSourceRevision(informationalVersion);

        // Assert
        Assert.Equal(expected, version);
    }

    [Fact]
    public void Version_ShouldComeFromAssemblyWithoutCommitId()
    {
        // Assert
        Assert.NotEmpty(AppInfo.Version);
        Assert.DoesNotContain('+', AppInfo.Version);
    }
}
