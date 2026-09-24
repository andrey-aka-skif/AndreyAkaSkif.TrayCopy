namespace AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;

/// <summary>
/// Представляет параметры хранилища <see cref="JsonSettingsStore"/>
/// </summary>
internal sealed class JsonSettingsStoreOptions
{
    /// <summary>
    /// Путь к файлу настроек; по умолчанию — файл в профиле текущего пользователя
    /// </summary>
    public string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AndreyAkaSkif.TrayCopy",
        "settings.json");
}
