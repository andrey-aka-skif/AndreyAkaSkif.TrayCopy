namespace AndreyAkaSkif.TrayCopy.Services;

/// <summary>
/// Обеспечивает запуск приложения в одной копии в пределах сеанса пользователя
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\AndreyAkaSkif.TrayCopy";

    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>
    /// Захватывает право на запуск; возвращает <see langword="null"/>, если приложение уже
    /// запущено. Право действует до вызова <see cref="Dispose"/> в том же потоке
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstance(mutex);
        }

        mutex.Dispose();
        return null;
    }

    /// <summary>
    /// Освобождает право на запуск
    /// </summary>
    public void Dispose()
    {
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
