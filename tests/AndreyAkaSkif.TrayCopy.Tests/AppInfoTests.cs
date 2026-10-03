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

    [Fact]
    public void Copyright_ShouldComeFromAssembly()
    {
        // Assert
        Assert.NotEmpty(AppInfo.Copyright);
    }

    [Fact]
    public void RepositoryUrl_ShouldBeAbsoluteHttps()
    {
        // Assert
        Assert.True(AppInfo.RepositoryUrl.IsAbsoluteUri);
        Assert.Equal(Uri.UriSchemeHttps, AppInfo.RepositoryUrl.Scheme);
    }

    // Файлы копирует сборка приложения; тест ловит расхождение их имён в проекте и в AppInfo
    [Fact]
    public void LicenseFiles_ShouldBeNextToApplication()
    {
        // Assert
        Assert.True(File.Exists(AppInfo.LicensePath), AppInfo.LicensePath);
        Assert.True(File.Exists(AppInfo.ThirdPartyNoticesPath), AppInfo.ThirdPartyNoticesPath);
    }
}
