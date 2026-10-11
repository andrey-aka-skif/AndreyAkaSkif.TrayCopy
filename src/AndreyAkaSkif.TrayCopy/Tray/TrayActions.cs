using AndreyAkaSkif.TrayCopy.Clipboard;
using AndreyAkaSkif.TrayCopy.Entries;
using AndreyAkaSkif.TrayCopy.Notifications;
using AndreyAkaSkif.TrayCopy.Settings;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Выполняет действия по кликам на иконке в трее и пунктам её меню: копирует текущую
/// запись, выбирает другую и сообщает о результате уведомлением
/// </summary>
/// <param name="settings">Текущие настройки: записи и выбор текущей</param>
/// <param name="clipboard">Буфер обмена, в который копируется значение записи</param>
/// <param name="notifications">Показывает уведомления о результате</param>
internal sealed class TrayActions(
    SettingsService settings, IClipboard clipboard, NotificationService notifications)
{
    /// <summary>
    /// Происходит, когда клик или пункт меню должен открыть окно настроек
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// Происходит, когда пункт меню должен завершить приложение
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Выполняет действие клика левой кнопкой: копирует значение текущей записи в буфер
    /// обмена, а с Ctrl выбирает следующую запись. Клик с Shift и любой клик при пустом
    /// списке запрашивают окно настроек
    /// </summary>
    public void HandleClick(bool isShiftPressed, bool isControlPressed)
    {
        var current = settings.Current.Entries.Current;
        if (isShiftPressed || current is null)
        {
            RequestSettings();
            return;
        }

        if (isControlPressed)
        {
            SelectNext();
        }
        else
        {
            Copy(current);
        }
    }

    /// <summary>
    /// Создаёт меню по текущим настройкам: записи с отметкой у текущей, действия кликов,
    /// окно настроек и выход. При пустом списке остаются только окно настроек и выход
    /// </summary>
    /// <returns>Группы пунктов; меню разделяет их чертой</returns>
    public IReadOnlyList<IReadOnlyList<TrayMenuItem>> CreateMenu()
    {
        TrayMenuItem[] app =
        [
            new("Настройки…", RequestSettings) { Gesture = "Shift+клик" },
            new("Выход", () => ExitRequested?.Invoke(this, EventArgs.Empty)),
        ];

        var entries = settings.Current.Entries;
        if (entries.Current is not { } current)
        {
            return [app];
        }

        return
        [
            [
                .. entries.Items.Select(entry =>
                    new TrayMenuItem(entry.Name, () => Select(entry.Name))
                    {
                        IsChecked = entry == current,
                    }),
            ],
            [
                new("Копировать текущую", () => Copy(current)) { Gesture = "Клик" },
                new("Следующая запись", SelectNext) { Gesture = "Ctrl+клик" },
            ],
            app,
        ];
    }

    private void RequestSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void Copy(Entry entry)
    {
        try
        {
            clipboard.SetText(entry.Value);
        }
        catch (IOException e)
        {
            notifications.Show(new Notification("Не удалось скопировать", e.Message));
            return;
        }

        notifications.Show(new Notification("Скопировано", entry.Name));
    }

    private void SelectNext()
    {
        if (TrySelect(entries => entries.SelectNext()))
        {
            var selected = settings.Current.Entries.Current!;
            notifications.Show(new Notification("Выбрано для копирования", selected.Name));
        }
    }

    // Запись, выбранную в меню, пользователь видит сам: об успехе уведомления нет
    private void Select(string name)
    {
        if (name != settings.Current.Entries.Current?.Name)
        {
            _ = TrySelect(entries => entries.Select(name));
        }
    }

    // Выбор сохраняется в настройках; если сохранить не удалось, текущая запись остаётся
    // прежней, а уведомление называет причину
    private bool TrySelect(Func<EntryList, EntryList> select)
    {
        try
        {
            settings.Update(current => current with { Entries = select(current.Entries) });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            notifications.Show(new Notification("Не удалось выбрать запись", e.Message));
            return false;
        }

        return true;
    }
}
