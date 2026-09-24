using System.Drawing;
using AndreyAkaSkif.TrayCopy.Interop;
using Avalonia.Platform;
using H.NotifyIcon.Core;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Показывает иконку приложения в трее и системные уведомления от её имени, сообщает о
/// кликах по ней
/// </summary>
/// <remarks>
/// Единственное место, где приложение работает с H.NotifyIcon. События приходят в потоке,
/// в котором вызван <see cref="Show"/>, — в приложении это поток интерфейса
/// </remarks>
internal sealed class TrayIconHost : IDisposable
{
    private static readonly Uri IconUri = new("avares://AndreyAkaSkif.TrayCopy/Assets/app.ico");

    // GUID иконки выводится из пути exe: Windows привязывает GUID к бинарнику, и отладочная
    // сборка не конфликтует с установленной копией
    private readonly TrayIcon _trayIcon = new();
    private Icon? _icon;

    /// <summary>
    /// Создаёт иконку; в трее она появляется после <see cref="Show"/>
    /// </summary>
    public TrayIconHost()
    {
        var window = _trayIcon.MessageWindow;
        window.MouseEventReceived += OnMouseEventReceived;
        window.TaskbarCreated += OnTaskbarCreated;
        window.DpiChanged += OnDpiChanged;
    }

    /// <summary>
    /// Происходит после клика левой или правой кнопкой по иконке
    /// </summary>
    public event EventHandler<TrayClickEventArgs>? Clicked;

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
    /// Заменяет подсказку, которая появляется при наведении на иконку
    /// </summary>
    public void SetToolTip(string text) => _trayIcon.UpdateToolTip(text);

    /// <summary>
    /// Показывает системное уведомление от имени иконки: без звука, в том числе в первый час
    /// после входа (это ответ на действие пользователя), и только сразу — отложенное
    /// уведомление о клике устарело бы
    /// </summary>
    public void ShowNotification(string title, string text)
    {
        _trayIcon.ShowNotification(
            title,
            text,
            NotificationIcon.None,
            sound: false,
            respectQuietTime: false,
            realtime: true);

        // H.NotifyIcon 2.4.1 показывает уведомление без подсказки (NIF_TIP), и Explorer
        // перестаёт выводить её при наведении, пока подсказку не запишут заново. Исправлено
        // в H.NotifyIcon 2.5.0 (HavenDV/H.NotifyIcon#239)
        _trayIcon.UpdateToolTip(_trayIcon.ToolTip);
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

    // Средний клик не обрабатывается: Windows 11 начиная с 22621.1344 присылает его как
    // левый, уже после отпускания кнопки, и отличить их нельзя
    private void OnMouseEventReceived(object? sender, MessageWindow.MouseEventReceivedEventArgs e)
    {
        TrayButton? button = e.MouseEvent switch
        {
            MouseEvent.IconLeftMouseUp => TrayButton.Left,
            MouseEvent.IconRightMouseUp => TrayButton.Right,
            _ => null,
        };

        if (button is { } clicked)
        {
            Clicked?.Invoke(this, new TrayClickEventArgs(clicked, IsShiftPressed()));
        }
    }

    // Состояние клавиши в момент вызова, а не по очереди сообщений: сообщение трея приходит
    // от Explorer, и GetKeyState может вернуть устаревшее состояние
    private static bool IsShiftPressed() =>
        (NativeMethods.GetAsyncKeyState(NativeMethods.VkShift) & 0x8000) != 0;

    // Explorer перезапущен: его новый экземпляр ничего не знает о прежних иконках
    private void OnTaskbarCreated(object? sender, EventArgs e)
    {
        _ = _trayIcon.TryRemove();
        _trayIcon.Create();
    }

    private void OnDpiChanged(object? sender, EventArgs e) => LoadIcon();
}
