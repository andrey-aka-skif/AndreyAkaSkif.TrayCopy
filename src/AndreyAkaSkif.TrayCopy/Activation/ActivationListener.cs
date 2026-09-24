using AndreyAkaSkif.TrayCopy.Views;
using Avalonia.Threading;

namespace AndreyAkaSkif.TrayCopy.Activation;

/// <summary>
/// Ждёт просьб повторного запуска и показывает в ответ окно настроек
/// </summary>
/// <param name="settingsWindow">Показывает окно настроек</param>
internal sealed class ActivationListener(SettingsWindowService settingsWindow) : IDisposable
{
    private EventWaitHandle? _activation;
    private RegisteredWaitHandle? _registration;

    /// <summary>
    /// Начинает ждать просьб
    /// </summary>
    public void Start()
    {
        // Событие создано при запуске (SingleInstance) и здесь открывается по имени
        _activation = new EventWaitHandle(
            initialState: false, EventResetMode.AutoReset, SingleInstance.ActivationEventName);
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _activation,
            OnActivationRequested,
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    /// <summary>
    /// Прекращает ждать просьб
    /// </summary>
    public void Dispose()
    {
        _registration?.Unregister(null);
        _activation?.Dispose();
    }

    // Вызывается в потоке пула; окно показывается в потоке интерфейса
    private void OnActivationRequested(object? state, bool timedOut) =>
        Dispatcher.UIThread.Post(settingsWindow.Show);
}
