using AndreyAkaSkif.TrayCopy.Interop;
using AndreyAkaSkif.TrayCopy.Settings;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AndreyAkaSkif.TrayCopy.Notifications.Popup;

/// <summary>
/// Показывает уведомление собственным окном в правом нижнем углу рабочей области основного
/// экрана
/// </summary>
/// <remarks>
/// Окно одно: уведомление, пришедшее, пока окно на экране, заменяет текст и продлевает
/// показ. Окно не забирает фокус и при каждом показе выводится поверх всех окон, в том числе
/// окон «поверх всех»; клик по нему скрывает его. Используется из потока интерфейса
/// </remarks>
internal sealed class PopupNotifier : INotifier, IDisposable
{
    // Отступ окна от краёв рабочей области, в единицах интерфейса Avalonia
    private const double EdgeMargin = 12;

    private static readonly TimeSpan DisplayTime = TimeSpan.FromSeconds(1.5);

    private NotificationWindow? _window;
    private DispatcherTimer? _timer;

    /// <summary>
    /// Вид уведомления: собственное окно
    /// </summary>
    public NotificationKind Kind => NotificationKind.Popup;

    /// <summary>
    /// Показывает <paramref name="notification"/> и скрывает окно через полторы секунды;
    /// показ во время показа отсчитывает время заново
    /// </summary>
    public void Show(Notification notification)
    {
        var window = _window ??= CreateWindow();
        var timer = _timer ??= CreateTimer();

        window.DataContext = notification;
        Place(window);
        window.Show();
        RaiseAboveTopmost(window);

        timer.Stop();
        timer.Start();
    }

    /// <summary>
    /// Останавливает таймер и закрывает окно
    /// </summary>
    public void Dispose()
    {
        _timer?.Stop();
        _window?.Close();
    }

    private NotificationWindow CreateWindow()
    {
        var window = new NotificationWindow();

        // Высота окна следует за текстом, а нижний край остаётся на месте. Первый раз размер
        // становится известен при показе, до вывода окна на экран
        window.SizeChanged += (_, _) => Place(window);
        window.PointerPressed += (_, _) => Hide();
        window.Closed += (_, _) => _window = null;
        return window;
    }

    private DispatcherTimer CreateTimer()
    {
        var timer = new DispatcherTimer { Interval = DisplayTime };
        timer.Tick += (_, _) => Hide();
        return timer;
    }

    private void Hide()
    {
        _timer?.Stop();
        _window?.Hide();
    }

    // Topmost окна применяется при его создании, а показ без активации оставляет окно на
    // прежнем месте Z-порядка: окна «поверх всех», поднятые с прошлого показа, перекрыли бы
    // его. Поэтому окно поднимается наверх при каждом показе
    private static void RaiseAboveTopmost(Window window)
    {
        if (window.TryGetPlatformHandle() is not { } handle)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            handle.Handle, NativeMethods.HwndTopmost, x: 0, y: 0, width: 0, height: 0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    private static void Place(Window window)
    {
        var screen = window.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var area = screen.WorkingArea;
        var size = PixelSize.FromSize(window.ClientSize, screen.Scaling);
        var margin = (int)Math.Round(EdgeMargin * screen.Scaling);
        window.Position = new PixelPoint(
            area.Right - size.Width - margin, area.Bottom - size.Height - margin);
    }
}
