using AndreyAkaSkif.TrayCopy.Settings;

namespace AndreyAkaSkif.TrayCopy.Notifications;

/// <summary>
/// Определяет, как показывается уведомление одного вида
/// </summary>
internal interface INotifier
{
    /// <summary>
    /// Вид уведомления, который показывает уведомитель
    /// </summary>
    NotificationKind Kind { get; }

    /// <summary>
    /// Показывает уведомление
    /// </summary>
    void Show(Notification notification);
}
