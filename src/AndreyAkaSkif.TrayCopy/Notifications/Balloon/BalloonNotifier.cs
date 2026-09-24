using AndreyAkaSkif.TrayCopy.Settings;
using AndreyAkaSkif.TrayCopy.Tray;

namespace AndreyAkaSkif.TrayCopy.Notifications.Balloon;

/// <summary>
/// Показывает системное уведомление Windows от имени иконки в трее
/// </summary>
/// <param name="trayIcon">Иконка, от имени которой показывается уведомление</param>
internal sealed class BalloonNotifier(TrayIconHost trayIcon) : INotifier
{
    /// <summary>
    /// Вид уведомления: системное
    /// </summary>
    public NotificationKind Kind => NotificationKind.System;

    /// <summary>
    /// Показывает <paramref name="notification"/> системным уведомлением
    /// </summary>
    public void Show(Notification notification) =>
        trayIcon.ShowNotification(notification.Title, notification.Text);
}
