using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class GitHubReleaseSourceTests : IDisposable
{
    private static readonly Uri LatestReleaseUrl =
        new("https://api.github.com/repos/owner/repo/releases/latest");

    // Ответ API сокращён: лишние поля, как и в настоящем, должны пропускаться
    private const string ReleaseJson = """
        {
          "url": "https://api.github.com/repos/owner/repo/releases/1",
          "html_url": "https://github.com/owner/repo/releases/tag/v0.3.0",
          "tag_name": "v0.3.0",
          "name": "v0.3.0",
          "draft": false,
          "prerelease": false,
          "published_at": "2026-10-04T02:00:15Z",
          "assets": []
        }
        """;

    private readonly List<HttpClient> _clients = [];

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task GetLatestAsync_ShouldReturnVersionFromTagAndReleasePage()
    {
        // Arrange
        var (source, _) = CreateSource(_ => Respond(HttpStatusCode.OK, ReleaseJson));

        // Act
        var release = await source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(release);
        Assert.Equal(ReleaseVersion.Parse("0.3.0"), release.Version);
        Assert.Equal(new Uri("https://github.com/owner/repo/releases/tag/v0.3.0"), release.PageUrl);
    }

    [Fact]
    public async Task GetLatestAsync_ShouldSendGitHubApiRequest()
    {
        // Arrange
        var (source, handler) = CreateSource(_ => Respond(HttpStatusCode.OK, ReleaseJson));

        // Act
        await source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(LatestReleaseUrl, request.RequestUri);
        Assert.Equal(
            "application/vnd.github+json", Assert.Single(request.Headers.Accept).MediaType);
        Assert.Equal(
            "2026-03-10", Assert.Single(request.Headers.GetValues("X-GitHub-Api-Version")));
        var product = Assert.Single(request.Headers.UserAgent).Product;
        Assert.Equal("TrayCopy", product?.Name);
        Assert.Equal(AppInfo.Version, product?.Version);
    }

    // Без выпусков (и без полных выпусков — только черновики и пререлизы) API отвечает 404
    [Fact]
    public async Task GetLatestAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var (source, _) = CreateSource(_ => Respond(HttpStatusCode.NotFound, "{}"));

        // Act
        var release = await source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(release);
    }

    // 403 и 429 — исчерпан лимит анонимных запросов
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetLatestAsync_ShouldThrowHttpRequestException_OnErrorStatus(
        HttpStatusCode status)
    {
        // Arrange
        var (source, _) = CreateSource(_ => Respond(status, "{}"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => source.GetLatestAsync(TestContext.Current.CancellationToken));
        Assert.Equal(status, exception.StatusCode);
    }

    [Theory]
    [InlineData("0.3.0")]
    [InlineData("v0.3")]
    [InlineData("release-1")]
    public async Task GetLatestAsync_ShouldThrowFormatException_WhenTagIsNotVersion(string tag)
    {
        // Arrange
        var json = ReleaseJson.Replace("\"tag_name\": \"v0.3.0\"", $"\"tag_name\": \"{tag}\"",
            StringComparison.Ordinal);
        var (source, _) = CreateSource(_ => Respond(HttpStatusCode.OK, json));

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            () => source.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("""{ "html_url": "https://github.com/owner/repo/releases/tag/v0.3.0" }""")]
    [InlineData("""{ "tag_name": "v0.3.0" }""")]
    [InlineData("""{ "tag_name": null, "html_url": "https://github.com/" }""")]
    [InlineData("null")]
    [InlineData("<html></html>")]
    public async Task GetLatestAsync_ShouldThrowJsonException_WhenResponseIsIncomplete(
        string body)
    {
        // Arrange
        var (source, _) = CreateSource(_ => Respond(HttpStatusCode.OK, body));

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(
            () => source.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestAsync_ShouldThrowTimeoutException_WhenNoResponseInTime()
    {
        // Arrange
        var (source, _) = CreateSource(
            async cancellationToken =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Ответ после отмены");
            },
            timeout: TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => source.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    // Отмена вызывающим — не таймаут: окно закрыто, и сбоем это не считается
    [Fact]
    public async Task GetLatestAsync_ShouldThrowOperationCanceled_WhenCallerCancels()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var (source, _) = CreateSource(async cancellationToken =>
        {
            await cancellation.CancelAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Ответ после отмены");
        });

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.GetLatestAsync(cancellation.Token));
    }

    [Fact]
    public void LatestReleaseUrl_ShouldDefaultToApplicationRepository()
    {
        // Arrange
        const string expected =
            "https://api.github.com/repos/andrey-aka-skif/AndreyAkaSkif.TrayCopy/releases/latest";

        // Assert
        Assert.Equal(new Uri(expected), new GitHubReleaseSourceOptions().LatestReleaseUrl);
    }

    private (GitHubReleaseSource Source, FakeHttpMessageHandler Handler) CreateSource(
        Func<CancellationToken, Task<HttpResponseMessage>> respond, TimeSpan? timeout = null)
    {
        var handler = new FakeHttpMessageHandler(
            (_, cancellationToken) => respond(cancellationToken));
        var client = new HttpClient(handler);
        _clients.Add(client);

        var options = new GitHubReleaseSourceOptions { LatestReleaseUrl = LatestReleaseUrl };
        if (timeout is { } value)
        {
            options.Timeout = value;
        }

        return (new GitHubReleaseSource(client, Options.Create(options)), handler);
    }

    private static Task<HttpResponseMessage> Respond(HttpStatusCode status, string json) =>
        Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
}
