using AndreyAkaSkif.TrayCopy.Entries;

namespace AndreyAkaSkif.TrayCopy.Settings;

/// <summary>
/// Представляет настройки приложения: список записей, вид уведомления и защиту значений.
/// Значения записей в модели — всегда открытый текст; шифрует их хранилище
/// </summary>
internal sealed record AppSettings
{
    /// <summary>
    /// Список записей с текущей записью
    /// </summary>
    public EntryList Entries { get; init; } = EntryList.Empty;

    /// <summary>
    /// Вид уведомления
    /// </summary>
    public NotificationKind Notification { get; init; } = NotificationKind.Popup;

    /// <summary>
    /// Вид, в котором хранилище сохраняет значения записей
    /// </summary>
    public ProtectionMode Protection { get; init; } = ProtectionMode.Dpapi;
}
