using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Updates;

/// <summary>
/// Проверяет, вышла ли версия новее работающей. От способа публикации не зависит: сведения о
/// выпусках даёт <see cref="IReleaseSource"/>
/// </summary>
/// <param name="source">Источник сведений о выпусках</param>
/// <param name="options">Параметры: версия работающего приложения</param>
internal sealed class UpdateChecker(IReleaseSource source, IOptions<UpdateCheckerOptions> options)
{
    private readonly UpdateCheckerOptions _options = options.Value;

    /// <summary>
    /// Ищет обновление: возвращает последний выпуск, если он новее работающей версии, иначе
    /// <see langword="null"/>. Исключения источника выпусков пробрасываются
    /// </summary>
    /// <exception cref="FormatException">
    /// Версия работающего приложения не в формате X.Y.Z[-метка]
    /// </exception>
    public async Task<ReleaseInfo?> FindUpdateAsync(CancellationToken cancellationToken)
    {
        // Версия разбирается при проверке, а не при создании: сборка с версией не по SemVer
        // получает сбой проверки, а не падение при открытии окна
        var current = ReleaseVersion.Parse(_options.CurrentVersion);
        var latest = await source.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        return latest is not null && latest.Version > current ? latest : null;
    }
}
