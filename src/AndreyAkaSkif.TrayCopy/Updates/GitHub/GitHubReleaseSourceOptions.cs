namespace AndreyAkaSkif.TrayCopy.Updates.GitHub;

/// <summary>
/// Представляет параметры источника выпусков <see cref="GitHubReleaseSource"/>
/// </summary>
internal sealed class GitHubReleaseSourceOptions
{
    // Владелец и имя репозитория — путь его адреса: /владелец/имя
    private static readonly string RepositoryPath = AppInfo.RepositoryUrl.AbsolutePath.TrimEnd('/');

    /// <summary>
    /// Адрес запроса последнего выпуска в GitHub REST API; по умолчанию — для репозитория
    /// приложения
    /// </summary>
    public Uri LatestReleaseUrl { get; set; } = new(
        $"https://api.github.com/repos{RepositoryPath}/releases/latest");

    /// <summary>
    /// Наибольшее время ожидания ответа: окно не должно долго показывать идущую проверку
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
