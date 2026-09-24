namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет автозапуск в памяти: хранит состояние и запоминает вызовы
/// <see cref="SetEnabled"/>
/// </summary>
internal sealed class FakeAutostart(bool isEnabled = false) : IAutostart
{
    /// <summary>
    /// Значения, переданные в <see cref="SetEnabled"/>, в порядке вызовов
    /// </summary>
    public List<bool> Calls { get; } = [];

    public bool IsEnabled { get; private set; } = isEnabled;

    public void SetEnabled(bool enabled)
    {
        Calls.Add(enabled);
        IsEnabled = enabled;
    }
}
