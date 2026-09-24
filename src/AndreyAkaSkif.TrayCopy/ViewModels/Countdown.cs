using CommunityToolkit.Mvvm.ComponentModel;

namespace AndreyAkaSkif.TrayCopy.ViewModels;

/// <summary>
/// Представляет обратный отсчёт до скрытия окна
/// </summary>
/// <remarks>
/// Изменения свойств и событие <see cref="Elapsed"/> приходят в контексте синхронизации,
/// в котором вызван <see cref="Start"/>, — в приложении это поток интерфейса. Без
/// контекста они приходят в потоке таймера
/// </remarks>
/// <param name="timeProvider">Источник времени и таймеров</param>
internal sealed partial class Countdown(TimeProvider timeProvider) : ObservableObject, IDisposable
{
    // Шаг обновления: сектор убывает плавно, а таймер почти не нагружает процессор
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);

    private ITimer? _timer;
    private long _startTimestamp;
    private TimeSpan _duration;

    // Номер запуска: тик, отправленный в контекст до остановки, не должен сработать после неё
    private int _run;

    /// <summary>
    /// Происходит, когда время истекло; после отмены не происходит
    /// </summary>
    public event EventHandler? Elapsed;

    /// <summary>
    /// Признак идущего отсчёта
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; private set; }

    /// <summary>
    /// Оставшееся время в секундах, с округлением вверх
    /// </summary>
    [ObservableProperty]
    public partial int RemainingSeconds { get; private set; }

    /// <summary>
    /// Доля оставшегося времени: 1 в начале отсчёта, 0 в конце
    /// </summary>
    [ObservableProperty]
    public partial double RemainingFraction { get; private set; }

    /// <summary>
    /// Начинает отсчёт длительностью <paramref name="duration"/>; идущий отсчёт начинается
    /// заново
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Длительность не больше нуля</exception>
    public void Start(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);

        Stop();

        var run = _run;
        var context = SynchronizationContext.Current;
        _duration = duration;
        _startTimestamp = timeProvider.GetTimestamp();
        SetRemaining(duration);
        IsRunning = true;

        _timer = timeProvider.CreateTimer(
            _ =>
            {
                if (context is null)
                {
                    Tick(run);
                }
                else
                {
                    context.Post(_ => Tick(run), null);
                }
            },
            state: null,
            TickInterval,
            TickInterval);
    }

    /// <summary>
    /// Останавливает отсчёт; <see cref="Elapsed"/> не происходит
    /// </summary>
    public void Cancel() => Stop();

    /// <summary>
    /// Останавливает отсчёт и освобождает таймер
    /// </summary>
    public void Dispose() => Stop();

    private void Tick(int run)
    {
        if (run != _run)
        {
            return;
        }

        var remaining = _duration - timeProvider.GetElapsedTime(_startTimestamp);
        if (remaining > TimeSpan.Zero)
        {
            SetRemaining(remaining);
            return;
        }

        SetRemaining(TimeSpan.Zero);
        Stop();
        Elapsed?.Invoke(this, EventArgs.Empty);
    }

    private void SetRemaining(TimeSpan remaining)
    {
        RemainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        RemainingFraction = remaining / _duration;
    }

    private void Stop()
    {
        _run++;
        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
    }
}
