using AndreyAkaSkif.TrayCopy.Clipboard;
using AndreyAkaSkif.TrayCopy.Entries;
using AndreyAkaSkif.TrayCopy.Notifications;
using AndreyAkaSkif.TrayCopy.Settings;

namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Выполняет действия по кликам на иконке в трее: копирует текущую запись, выбирает
/// следующую и сообщает о результате уведомлением
/// </summary>
/// <param name="settings">Текущие настройки: записи и выбор текущей</param>
/// <param name="clipboard">Буфер обмена, в который копируется значение записи</param>
/// <param name="notifications">Показывает уведомления о результате</param>
internal sealed class TrayActions(
    SettingsService settings, IClipboard clipboard, NotificationService notifications)
{
    /// <summary>
    /// Происходит, когда клик должен открыть окно настроек
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// Выполняет действие клика кнопкой <paramref name="button"/>: левая копирует значение
    /// текущей записи в буфер обмена, правая выбирает следующую запись. Клик с Shift и любой
    /// клик при пустом списке запрашивают окно настроек
    /// </summary>
    public void HandleClick(TrayButton button, bool isShiftPressed)
    {
        var current = settings.Current.Entries.Current;
        if (isShiftPressed || current is null)
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (button == TrayButton.Left)
        {
            Copy(current);
        }
        else
        {
            SelectNext();
        }
    }

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

    // Выбор сохраняется в настройках; если сохранить не удалось, текущая запись остаётся
    // прежней
    private void SelectNext()
    {
        try
        {
            settings.Update(current => current with { Entries = current.Entries.SelectNext() });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            notifications.Show(new Notification("Не удалось выбрать запись", e.Message));
            return;
        }

        var selected = settings.Current.Entries.Current!;
        notifications.Show(new Notification("Выбрано для копирования", selected.Name));
    }
}
