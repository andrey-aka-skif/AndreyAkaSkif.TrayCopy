namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет хранилище настроек в памяти: отдаёт заданный результат чтения и
/// запоминает сохранённые настройки
/// </summary>
internal sealed class InMemorySettingsStore(SettingsLoadResult loadResult) : ISettingsStore
{
    /// <summary>
    /// Настройки в порядке сохранения
    /// </summary>
    public List<AppSettings> Saved { get; } = [];

    /// <summary>
    /// Исключение, которое бросает сохранение; <see langword="null"/> — сохранение успешно
    /// </summary>
    public Exception? SaveException { get; set; }

    public SettingsLoadResult Load() => loadResult;

    public void Save(AppSettings settings)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        Saved.Add(settings);
    }
}
