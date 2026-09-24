using AndreyAkaSkif.TrayCopy.Entries;

namespace AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;

/// <summary>
/// Представляет содержимое файла настроек
/// </summary>
/// <param name="Version">Версия формата файла</param>
/// <param name="Notification">Вид уведомления</param>
/// <param name="Protection">Вид, в котором записаны значения записей</param>
/// <param name="StartupDisplaySeconds">Время показа окна настроек при запуске, в секундах</param>
/// <param name="TrimWhitespace">Признак обрезки пробелов по краям при сохранении</param>
/// <param name="Selected">Имя текущей записи</param>
/// <param name="Entries">Записи; значения — в виде, заданном <paramref name="Protection"/></param>
internal sealed record SettingsFile(
    int Version,
    NotificationKind Notification,
    ProtectionMode Protection,
    int StartupDisplaySeconds,
    bool TrimWhitespace,
    string? Selected,
    IReadOnlyList<Entry> Entries)
{
    /// <summary>
    /// Версия формата файла, которую пишет и читает приложение
    /// </summary>
    public const int CurrentVersion = 1;
}
