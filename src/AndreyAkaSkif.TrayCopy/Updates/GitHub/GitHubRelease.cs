namespace AndreyAkaSkif.TrayCopy.Updates.GitHub;

/// <summary>
/// Представляет выпуск в ответе GitHub REST API; читаются только нужные поля
/// </summary>
/// <param name="TagName">Тег выпуска</param>
/// <param name="HtmlUrl">Адрес страницы выпуска на GitHub</param>
internal sealed record GitHubRelease(string TagName, Uri HtmlUrl);
