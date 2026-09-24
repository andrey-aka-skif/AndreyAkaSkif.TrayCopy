using Avalonia.Controls;

namespace AndreyAkaSkif.TrayCopy.Notifications.Popup;

/// <summary>
/// Представляет окно уведомления: заголовок и текст рядом с иконкой приложения
/// </summary>
internal sealed partial class NotificationWindow : Window
{
    /// <summary>
    /// Создаёт окно; уведомление передаётся через <see cref="Avalonia.StyledElement.DataContext"/>
    /// </summary>
    public NotificationWindow()
    {
        InitializeComponent();
    }
}
