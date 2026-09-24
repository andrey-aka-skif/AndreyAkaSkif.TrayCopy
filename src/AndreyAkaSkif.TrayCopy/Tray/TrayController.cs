using AndreyAkaSkif.TrayCopy.Views;
using Avalonia.Threading;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Связывает иконку в трее с действиями приложения: клик с Shift открывает окно настроек
/// </summary>
internal sealed class TrayController
{
    private readonly TrayIconHost _trayIcon;
    private readonly SettingsWindowService _settingsWindow;

    /// <summary>
    /// Создаёт контроллер иконки <paramref name="trayIcon"/>; окно настроек он открывает
    /// через <paramref name="settingsWindow"/>
    /// </summary>
    public TrayController(TrayIconHost trayIcon, SettingsWindowService settingsWindow)
    {
        _trayIcon = trayIcon;
        _settingsWindow = settingsWindow;

        trayIcon.Clicked += OnClicked;
    }

    /// <summary>
    /// Показывает иконку в трее
    /// </summary>
    public void Show() => _trayIcon.Show();

    private void OnClicked(object? sender, TrayClickEventArgs e)
    {
        if (e.IsShiftPressed)
        {
            // Окно показывается после выхода из обработчика сообщения трея
            Dispatcher.UIThread.Post(_settingsWindow.Show);
        }
    }
}
