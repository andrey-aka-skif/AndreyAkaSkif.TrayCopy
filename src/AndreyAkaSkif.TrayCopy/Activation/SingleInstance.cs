using AndreyAkaSkif.TrayCopy.Interop;

namespace AndreyAkaSkif.TrayCopy.Activation;

/// <summary>
/// Обеспечивает запуск приложения в одной копии в пределах сеанса пользователя. Повторный
/// запуск не открывает вторую копию, а просит работающую показать окно настроек
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    /// <summary>
    /// Имя события, которым повторный запуск просит работающую копию показать окно настроек
    /// </summary>
    public const string ActivationEventName = @"Local\AndreyAkaSkif.TrayCopy.Activate";

    private const string MutexName = @"Local\AndreyAkaSkif.TrayCopy";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;

    private SingleInstance(Mutex mutex, EventWaitHandle activation)
    {
        _mutex = mutex;
        _activation = activation;
    }

    /// <summary>
    /// Захватывает право на запуск; возвращает <see langword="null"/>, если приложение уже
    /// запущено. Право действует до вызова <see cref="Dispose"/> в том же потоке
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        // Событие создаётся раньше мьютекса и живёт, пока жива копия: если мьютекс занят,
        // событие уже есть, и просьба повторного запуска не теряется, даже когда первая
        // копия ещё не начала её ждать
        var activation = new EventWaitHandle(
            initialState: false, EventResetMode.AutoReset, ActivationEventName);
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstance(mutex, activation);
        }

        mutex.Dispose();
        activation.Dispose();
        return null;
    }

    /// <summary>
    /// Просит работающую копию показать окно настроек. Если копия успела завершиться,
    /// ничего не делает
    /// </summary>
    public static void RequestActivation()
    {
        if (!EventWaitHandle.TryOpenExisting(ActivationEventName, out var activation))
        {
            return;
        }

        using (activation)
        {
            // Право вывести окно вперёд есть у процесса, который запустил пользователь, —
            // у этого. Без передачи права окно работающей копии может остаться позади
            NativeMethods.AllowSetForegroundWindow(NativeMethods.AsfwAny);
            activation.Set();
        }
    }

    /// <summary>
    /// Освобождает право на запуск
    /// </summary>
    public void Dispose()
    {
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        _activation.Dispose();
    }
}
