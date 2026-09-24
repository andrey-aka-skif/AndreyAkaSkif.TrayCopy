using AndreyAkaSkif.TrayCopy.Settings.Persistence;

namespace AndreyAkaSkif.TrayCopy.Settings;

/// <summary>
/// Хранит текущие настройки приложения и сохраняет их изменения. Потребители берут
/// <see cref="Current"/> в момент использования, а не держат у себя копию
/// </summary>
/// <remarks>
/// Не потокобезопасен: используется из потока интерфейса
/// </remarks>
internal sealed class SettingsService
{
    private readonly ISettingsStore _store;

    /// <summary>
    /// Создаёт сервис и читает настройки из <paramref name="store"/>
    /// </summary>
    public SettingsService(ISettingsStore store)
    {
        _store = store;

        var result = store.Load();
        Current = result.Settings;
        BackupPath = result.BackupPath;
    }

    /// <summary>
    /// Происходит после сохранения изменённых настроек
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Текущие настройки
    /// </summary>
    public AppSettings Current { get; private set; }

    /// <summary>
    /// Расположение резервной копии, в которую при чтении отложены нечитаемые настройки;
    /// <see langword="null"/>, если настройки прочитаны или их не было
    /// </summary>
    public string? BackupPath { get; }

    /// <summary>
    /// Применяет <paramref name="change"/> к текущим настройкам, сохраняет результат и
    /// сообщает об изменении. Если сохранить не удалось, текущие настройки не меняются
    /// </summary>
    public void Update(Func<AppSettings, AppSettings> change)
    {
        var settings = change(Current);
        _store.Save(settings);

        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
