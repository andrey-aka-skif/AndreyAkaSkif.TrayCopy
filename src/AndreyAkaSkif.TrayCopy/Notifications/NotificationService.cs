using AndreyAkaSkif.TrayCopy.Settings;

namespace AndreyAkaSkif.TrayCopy.Notifications;

/// <summary>
/// Показывает уведомления тем видом, который выбран в настройках на момент показа
/// </summary>
/// <param name="settings">Текущие настройки: из них берётся вид уведомления</param>
/// <param name="notifiers">Уведомители, по одному на каждый вид</param>
internal sealed class NotificationService(
    SettingsService settings, IEnumerable<INotifier> notifiers)
{
    private readonly INotifier[] _notifiers = [.. notifiers];

    /// <summary>
    /// Показывает <paramref name="notification"/> уведомителем выбранного вида
    /// </summary>
    public void Show(Notification notification)
    {
        var kind = settings.Current.Notification;
        _notifiers.Single(notifier => notifier.Kind == kind).Show(notification);
    }
}
