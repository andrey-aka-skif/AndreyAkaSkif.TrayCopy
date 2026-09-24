namespace AndreyAkaSkif.TrayCopy.Settings.Persistence;

/// <summary>
/// Представляет результат чтения настроек
/// </summary>
/// <param name="Settings">Прочитанные настройки или настройки по умолчанию</param>
/// <param name="BackupPath">
/// Расположение резервной копии нечитаемых настроек (у файлового хранилища — путь к файлу);
/// <see langword="null"/>, если настройки прочитаны или их не было
/// </param>
internal sealed record SettingsLoadResult(AppSettings Settings, string? BackupPath);
