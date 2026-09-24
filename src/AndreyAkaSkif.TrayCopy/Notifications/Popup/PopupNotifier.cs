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
/// показ. Окно не забирает фокус; клик по нему скрывает его. Используется из потока
/// интерфейса
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
