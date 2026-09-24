namespace AndreyAkaSkif.TrayCopy.Settings;

/// <summary>
/// Определяет, чем показывается уведомление о копировании и выборе записи
/// </summary>
internal enum NotificationKind
{
    /// <summary>
    /// Собственное окно приложения
    /// </summary>
    Popup,

    /// <summary>
    /// Системное уведомление Windows
    /// </summary>
    System,
}
