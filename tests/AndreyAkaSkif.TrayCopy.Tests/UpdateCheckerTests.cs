using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Tests;

public class UpdateCheckerTests
{
    private readonly FakeReleaseSource _source = new();

    [Theory]
    [InlineData("0.2.0", "0.3.0")]
    [InlineData("0.2.0", "0.2.1")]
    [InlineData("0.2.1-dev.3", "0.2.1")]
    [InlineData("0.0.0-local", "0.2.0")]
    public async Task FindUpdateAsync_ShouldReturnRelease_WhenItIsNewer(
        string current, string latest)
    {
        // Arrange
        var release = FakeReleaseSource.Release(latest);
        _source.Latest = release;

        // Act
        var update = await CreateChecker(current)
            .FindUpdateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(release, update);
    }

    [Theory]
    [InlineData("0.2.0", "0.2.0")]
    [InlineData("0.2.0", "0.1.0")]
    [InlineData("0.2.1-dev.3", "0.2.0")]
    public async Task FindUpdateAsync_ShouldReturnNull_WhenReleaseIsNotNewer(
        string current, string latest)
    {
        // Arrange
        _source.Latest = FakeReleaseSource.Release(latest);

        // Act
        var update = await CreateChecker(current)
            .FindUpdateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(update);
    }

    [Fact]
    public async Task FindUpdateAsync_ShouldReturnNull_WhenThereAreNoReleases()
    {
        // Act
        var update = await CreateChecker("0.2.0")
            .FindUpdateAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(update);
    }

    [Fact]
    public async Task FindUpdateAsync_ShouldPropagateSourceException()
    {
        // Arrange
        var exception = new HttpRequestException("Нет сети");
        _source.Exception = exception;

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateChecker("0.2.0").FindUpdateAsync(TestContext.Current.CancellationToken));
        Assert.Same(exception, thrown);
    }

    // Версия сборки — из -p:Version; версия не по SemVer — сбой проверки, а не ложный ответ
    [Fact]
    public async Task FindUpdateAsync_ShouldThrowFormatException_WhenCurrentVersionIsNotSemVer()
    {
        // Arrange
        _source.Latest = FakeReleaseSource.Release("0.2.0");

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            () => CreateChecker("1.0").FindUpdateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CurrentVersion_ShouldDefaultToApplicationVersion()
    {
        // Assert
        Assert.Equal(AppInfo.Version, new UpdateCheckerOptions().CurrentVersion);
    }

    private UpdateChecker CreateChecker(string currentVersion) => new(
        _source, Options.Create(new UpdateCheckerOptions { CurrentVersion = currentVersion }));
}
