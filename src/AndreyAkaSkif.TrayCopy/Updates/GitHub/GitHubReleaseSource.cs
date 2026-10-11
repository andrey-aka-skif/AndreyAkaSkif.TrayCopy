using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Updates.GitHub;

/// <summary>
/// Получает сведения о выпусках приложения из GitHub Releases через REST API
/// </summary>
/// <remarks>
/// Запрос анонимный: о публичном репозитории API отвечает без токена, но не больше 60 раз в
/// час на один IP-адрес. Последний выпуск — самый новый релиз, кроме черновиков и
/// пререлизов; если таких нет, API отвечает 404. Тег выпуска — vX.Y.Z, как требует воркфлоу
/// выпуска
/// </remarks>
/// <param name="httpClient">
/// Общий клиент приложения. Заголовки GitHub задаются в каждом запросе, а не в самом
/// клиенте: другим потребителям они не нужны
/// </param>
/// <param name="options">Параметры: адрес запроса, время ожидания ответа</param>
internal sealed class GitHubReleaseSource(
    HttpClient httpClient,
    IOptions<GitHubReleaseSourceOptions> options) : IReleaseSource
{
    // Версия REST API, под ответ которой написан разбор, и её тип содержимого
    private const string ApiVersion = "2026-03-10";
    private const string MediaType = "application/vnd.github+json";

    private readonly GitHubReleaseSourceOptions _options = options.Value;

    /// <summary>
    /// Получает последний выпуск; <see langword="null"/>, если в репозитории нет выпусков
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Сбой сети или ответ с кодом ошибки, кроме 404
    /// </exception>
    /// <exception cref="TimeoutException">Ответ не пришёл за время ожидания</exception>
    /// <exception cref="JsonException">В ответе нет нужных полей</exception>
    /// <exception cref="FormatException">Тег выпуска не в формате vX.Y.Z</exception>
    /// <exception cref="OperationCanceledException">
    /// Отменён <paramref name="cancellationToken"/>
    /// </exception>
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, _options.LatestReleaseUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
        request.Headers.Add("X-GitHub-Api-Version", ApiVersion);

        // Запрос без User-Agent GitHub отклоняет
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TrayCopy", AppInfo.Version));

        try
        {
            using var response = await httpClient
                .SendAsync(request, timeout.Token)
                .ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var release = await response.Content
                .ReadFromJsonAsync(GitHubJsonContext.Default.GitHubRelease, timeout.Token)
                .ConfigureAwait(false)
                ?? throw new JsonException("Ответ GitHub пуст");

            return new ReleaseInfo(ParseTag(release.TagName), release.HtmlUrl);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"GitHub не ответил за {_options.Timeout.TotalSeconds:0.#} с");
        }
    }

    private static ReleaseVersion ParseTag(string tag) =>
        tag.StartsWith('v') && ReleaseVersion.TryParse(tag[1..], out var version)
            ? version
            : throw new FormatException($"Тег выпуска «{tag}» не в формате vX.Y.Z");
}
