namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет запуск кода без контекста синхронизации
/// </summary>
internal static class TestSynchronization
{
    /// <summary>
    /// Выполняет <paramref name="action"/> без контекста синхронизации и восстанавливает
    /// прежний. Нужен для <see cref="Countdown"/>: он захватывает контекст при запуске, а
    /// без контекста обрабатывает тики сразу, в потоке, который двигает время
    /// </summary>
    public static void RunWithoutContext(Action action)
    {
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }
}
