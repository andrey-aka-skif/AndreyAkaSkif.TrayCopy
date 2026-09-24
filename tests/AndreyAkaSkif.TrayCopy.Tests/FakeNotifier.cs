namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет уведомитель заданного вида, который запоминает показанные уведомления
/// </summary>
internal sealed class FakeNotifier(NotificationKind kind) : INotifier
{
    /// <summary>
    /// Уведомления в порядке показа
    /// </summary>
    public List<Notification> Shown { get; } = [];

    public NotificationKind Kind => kind;

    public void Show(Notification notification) => Shown.Add(notification);
}
