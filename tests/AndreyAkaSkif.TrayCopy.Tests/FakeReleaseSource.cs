namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет источник выпусков в памяти: отдаёт заданный выпуск, бросает заданное
/// исключение или ждёт, пока ответ не задан через <see cref="Pending"/>
/// </summary>
internal sealed class FakeReleaseSource : IReleaseSource
{
    /// <summary>
    /// Последний выпуск; <see langword="null"/> — выпусков нет
    /// </summary>
    public ReleaseInfo? Latest { get; set; }

    /// <summary>
    /// Исключение, которым завершается запрос; <see langword="null"/> — запрос успешен
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Ответ, который запрос ждёт до завершения или отмены; <see langword="null"/> — запрос
    /// отвечает сразу
    /// </summary>
    public TaskCompletionSource<ReleaseInfo?>? Pending { get; set; }

    /// <summary>
    /// Число запросов
    /// </summary>
    public int Calls { get; private set; }

    public Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken)
    {
        Calls++;
        if (Pending is not null)
        {
            return Pending.Task.WaitAsync(cancellationToken);
        }

        return Exception is null
            ? Task.FromResult(Latest)
            : Task.FromException<ReleaseInfo?>(Exception);
    }

    /// <summary>
    /// Создаёт выпуск версии <paramref name="version"/>
    /// </summary>
    public static ReleaseInfo Release(string version) => new(
        ReleaseVersion.Parse(version), new Uri($"https://example.com/releases/v{version}"));
}
