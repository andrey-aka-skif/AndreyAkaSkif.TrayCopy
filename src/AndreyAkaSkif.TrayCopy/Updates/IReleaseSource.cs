namespace AndreyAkaSkif.TrayCopy.Updates;

/// <summary>
/// Определяет, откуда приложение узнаёт о своих выпусках
/// </summary>
internal interface IReleaseSource
{
    /// <summary>
    /// Получает последний выпуск; <see langword="null"/>, если выпусков нет. Сбой получения
    /// сведений — исключение, тип которого зависит от реализации
    /// </summary>
    /// <exception cref="OperationCanceledException">
    /// Отменён <paramref name="cancellationToken"/>
    /// </exception>
    Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellationToken);
}
