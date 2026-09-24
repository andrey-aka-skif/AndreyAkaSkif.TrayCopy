using AndreyAkaSkif.TrayCopy.Settings;
using AndreyAkaSkif.TrayCopy.Views;
using Avalonia.Threading;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Связывает иконку в трее с действиями приложения: передаёт клики в
/// <see cref="TrayActions"/>, открывает по их запросу окно настроек и показывает текущую
/// запись в подсказке иконки
/// </summary>
internal sealed class TrayController
{
    private const string AppName = "TrayCopy";

    private readonly TrayIconHost _trayIcon;
    private readonly TrayActions _actions;
    private readonly SettingsService _settings;

    /// <summary>
    /// Создаёт контроллер иконки <paramref name="trayIcon"/>
    /// </summary>
    /// <param name="trayIcon">Иконка в трее</param>
    /// <param name="actions">Действия по кликам</param>
    /// <param name="settings">Текущие настройки: по ним строится подсказка</param>
    /// <param name="settingsWindow">Открывает окно настроек</param>
    public TrayController(
        TrayIconHost trayIcon,
        TrayActions actions,
        SettingsService settings,
        SettingsWindowService settingsWindow)
    {
        _trayIcon = trayIcon;
        _actions = actions;
        _settings = settings;

        trayIcon.Clicked += OnClicked;
        actions.SettingsRequested += (_, _) => settingsWindow.Show();
        settings.Changed += (_, _) => UpdateToolTip();
    }

    /// <summary>
    /// Показывает иконку в трее
    /// </summary>
    public void Show()
    {
        UpdateToolTip();
        _trayIcon.Show();
    }

    // Действие выполняется после выхода из обработчика сообщения трея
    private void OnClicked(object? sender, TrayClickEventArgs e) =>
        Dispatcher.UIThread.Post(() => _actions.HandleClick(e.Button, e.IsShiftPressed));

    private void UpdateToolTip()
    {
        var current = _settings.Current.Entries.Current;
        _trayIcon.SetToolTip(current is null ? AppName : $"{AppName} — {current.Name}");
    }
}
