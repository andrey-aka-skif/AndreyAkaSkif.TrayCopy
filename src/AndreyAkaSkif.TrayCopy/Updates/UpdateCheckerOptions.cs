namespace AndreyAkaSkif.TrayCopy.Updates;

/// <summary>
/// Представляет параметры проверки обновлений <see cref="UpdateChecker"/>
/// </summary>
internal sealed class UpdateCheckerOptions
{
    /// <summary>
    /// Версия работающего приложения; по умолчанию — из атрибутов сборки
    /// </summary>
    public string CurrentVersion { get; set; } = AppInfo.Version;
}
