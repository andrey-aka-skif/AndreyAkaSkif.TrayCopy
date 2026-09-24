using System.Drawing;
using AndreyAkaSkif.TrayCopy.Interop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using H.NotifyIcon.Core;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Управляет иконкой приложения в трее и обрабатывает клики по ней
/// </summary>
internal sealed class TrayController : IDisposable
{
    private const string ToolTip = "TrayCopy";

    private static readonly Uri IconUri = new("avares://AndreyAkaSkif.TrayCopy/Assets/app.ico");

    // GUID иконки выводится из пути exe: Windows привязывает GUID к бинарнику, и отладочная
    // сборка не конфликтует с установленной копией
    private readonly TrayIcon _trayIcon = new() { ToolTip = ToolTip };
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;
    private Icon? _icon;

    /// <summary>
    /// Создаёт контроллер; через <paramref name="lifetime"/> он завершает приложение
    /// </summary>
    public TrayController(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        _lifetime = lifetime;

        var window = _trayIcon.MessageWindow;
        window.MouseEventReceived += OnMouseEventReceived;
        window.TaskbarCreated += OnTaskbarCreated;
        window.DpiChanged += OnDpiChanged;
    }

    /// <summary>
    /// Показывает иконку в трее
    /// </summary>
    public void Show()
    {
        // Окно сообщений нужно раньше иконки: по нему определяется DPI
        _trayIcon.MessageWindow.Create();
        LoadIcon();
        _trayIcon.Create();
    }

    /// <summary>
    /// Убирает иконку из трея и освобождает её ресурсы
    /// </summary>
    public void Dispose()
    {
        _trayIcon.Dispose();
        _icon?.Dispose();
    }

    // Кадр из app.ico под размер маленькой иконки при текущем масштабе: 16 px при 100 %,
    // 20, 24, 32, 40 — при 125–250 %
    private void LoadIcon()
    {
        var dpi = NativeMethods.GetDpiForWindow(_trayIcon.MessageWindow.Handle);
        var size = NativeMethods.GetSystemMetricsForDpi(NativeMethods.SmCxSmIcon, dpi);

        using var stream = AssetLoader.Open(IconUri);
        var icon = new Icon(stream, size, size);
        _trayIcon.UpdateIcon(icon.Handle);

        _icon?.Dispose();
        _icon = icon;
    }

    private void OnMouseEventReceived(object? sender, MessageWindow.MouseEventReceivedEventArgs e)
    {
        if (e.MouseEvent == MouseEvent.IconMiddleMouseUp)
        {
            // Временный выход, пока нет окна настроек с кнопкой «Выйти». Отложен до выхода
            // из обработчика: завершение уничтожает окно, чьё сообщение сейчас обрабатывается
            Dispatcher.UIThread.Post(() => _lifetime.Shutdown());
        }
    }

    // Explorer перезапущен: его новый экземпляр ничего не знает о прежних иконках
    private void OnTaskbarCreated(object? sender, EventArgs e)
    {
        _ = _trayIcon.TryRemove();
        _trayIcon.Create();
    }

    private void OnDpiChanged(object? sender, EventArgs e) => LoadIcon();
}
