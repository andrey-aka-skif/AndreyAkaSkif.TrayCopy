using AndreyAkaSkif.TrayCopy.Entries;

namespace AndreyAkaSkif.TrayCopy.Settings;

/// <summary>
/// Представляет настройки приложения: список записей, вид уведомления, защиту значений,
/// показ окна настроек при запуске и обрезку пробелов. Значения записей в модели — всегда
/// открытый текст; шифрует их хранилище
/// </summary>
internal sealed record AppSettings
{
    /// <summary>
    /// Наименьшее время показа окна настроек при запуске: окно не показывается
    /// </summary>
    public static TimeSpan MinStartupDisplayTime { get; } = TimeSpan.Zero;

    /// <summary>
    /// Наибольшее время показа окна настроек при запуске
    /// </summary>
    public static TimeSpan MaxStartupDisplayTime { get; } = TimeSpan.FromSeconds(60);

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

    /// <summary>
    /// Время, на которое окно настроек показывается при запуске, если записи есть: целое
    /// число секунд от <see cref="MinStartupDisplayTime"/> до
    /// <see cref="MaxStartupDisplayTime"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Время вне допустимого диапазона или не кратно секунде
    /// </exception>
    public TimeSpan StartupDisplayTime
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinStartupDisplayTime);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxStartupDisplayTime);
            if (value.Ticks % TimeSpan.TicksPerSecond != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Время показа должно быть целым числом секунд");
            }

            field = value;
        }
    } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Признак того, что окно настроек обрезает пробелы по краям имён и значений при
    /// сохранении
    /// </summary>
    public bool TrimWhitespace { get; init; } = true;
}
