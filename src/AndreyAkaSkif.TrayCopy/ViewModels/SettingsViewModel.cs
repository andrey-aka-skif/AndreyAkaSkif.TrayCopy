using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security;
using AndreyAkaSkif.TrayCopy.Autostart;
using AndreyAkaSkif.TrayCopy.Entries;
using AndreyAkaSkif.TrayCopy.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndreyAkaSkif.TrayCopy.ViewModels;

/// <summary>
/// Представляет окно настроек: снимок текущих настроек и фактического автозапуска, который
/// пользователь правит и сохраняет или отбрасывает
/// </summary>
internal sealed partial class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly SettingsService _settings;
    private readonly IAutostart _autostart;
    private readonly bool _wasAutostartEnabled;

    /// <summary>
    /// Создаёт модель окна по текущим настройкам <paramref name="settings"/> и фактическому
    /// состоянию автозапуска <paramref name="autostart"/>
    /// </summary>
    public SettingsViewModel(
        SettingsService settings, IAutostart autostart, TimeProvider timeProvider)
    {
        _settings = settings;
        _autostart = autostart;

        if (settings.BackupPath is { } backupPath)
        {
            BackupFileName = Path.GetFileName(backupPath);
            BackupFolder = Path.GetDirectoryName(backupPath);
        }

        var current = settings.Current;
        TrimWhitespace = current.TrimWhitespace;
        Notification = current.Notification;
        IsProtected = current.Protection == ProtectionMode.Dpapi;
        StartupDisplaySeconds = (decimal)current.StartupDisplayTime.TotalSeconds;
        IsAutostartEnabled = _wasAutostartEnabled = autostart.IsEnabled;

        foreach (var entry in current.Entries.Items)
        {
            Entries.Add(new EntryViewModel(this, entry));
        }

        Entries.CollectionChanged += OnEntriesChanged;

        Countdown = new Countdown(timeProvider);
        Countdown.Elapsed += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Происходит, когда окно нужно закрыть: после сохранения, отмены или по истечении
    /// отсчёта
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Происходит, когда пользователь выходит из приложения
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Наименьшее время показа окна при запуске, в секундах
    /// </summary>
    public static decimal MinStartupDisplaySeconds { get; } =
        (decimal)AppSettings.MinStartupDisplayTime.TotalSeconds;

    /// <summary>
    /// Наибольшее время показа окна при запуске, в секундах
    /// </summary>
    public static decimal MaxStartupDisplaySeconds { get; } =
        (decimal)AppSettings.MaxStartupDisplayTime.TotalSeconds;

    /// <summary>
    /// Имя файла, в который при запуске приложения отложены нечитаемые настройки;
    /// <see langword="null"/>, если настройки прочитаны или их не было
    /// </summary>
    public string? BackupFileName { get; }

    /// <summary>
    /// Папка с отложенным файлом настроек; <see langword="null"/>, если такого файла нет
    /// </summary>
    public string? BackupFolder { get; }

    /// <summary>
    /// Строки списка записей в порядке переключения
    /// </summary>
    public ObservableCollection<EntryViewModel> Entries { get; } = [];

    /// <summary>
    /// Выделенная строка, к которой относятся удаление и перемещение
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    public partial EntryViewModel? SelectedEntry { get; set; }

    /// <summary>
    /// Признак обрезки пробелов по краям имён и значений при сохранении
    /// </summary>
    [ObservableProperty]
    public partial bool TrimWhitespace { get; set; }

    /// <summary>
    /// Вид уведомления
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPopupNotification))]
    [NotifyPropertyChangedFor(nameof(IsSystemNotification))]
    public partial NotificationKind Notification { get; set; }

    /// <summary>
    /// Признак уведомления собственным окном; для переключателя в окне
    /// </summary>
    public bool IsPopupNotification
    {
        get => Notification == NotificationKind.Popup;
        set => SelectNotification(NotificationKind.Popup, value);
    }

    /// <summary>
    /// Признак системного уведомления; для переключателя в окне
    /// </summary>
    public bool IsSystemNotification
    {
        get => Notification == NotificationKind.System;
        set => SelectNotification(NotificationKind.System, value);
    }

    /// <summary>
    /// Признак шифрования значений DPAPI
    /// </summary>
    [ObservableProperty]
    public partial bool IsProtected { get; set; }

    /// <summary>
    /// Признак запуска приложения при входе в Windows
    /// </summary>
    [ObservableProperty]
    public partial bool IsAutostartEnabled { get; set; }

    /// <summary>
    /// Время показа окна при запуске, в секундах
    /// </summary>
    [ObservableProperty]
    public partial decimal StartupDisplaySeconds { get; set; }

    /// <summary>
    /// Отсчёт до скрытия окна, показанного при запуске
    /// </summary>
    public Countdown Countdown { get; }

    /// <summary>
    /// Сообщение о неудачном сохранении; <see langword="null"/>, если сбоя не было
    /// </summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    /// <summary>
    /// Возвращает имена, под которыми будут сохранены строки списка, кроме
    /// <paramref name="entry"/>
    /// </summary>
    public IEnumerable<string> GetSavedNamesExcept(EntryViewModel entry) =>
        Entries.Where(other => other != entry).Select(other => other.SavedName);

    /// <summary>
    /// Перепроверяет имена всех строк: изменение одного имени может снять или создать
    /// совпадение у других
    /// </summary>
    public void ValidateNames()
    {
        foreach (var entry in Entries)
        {
            entry.ValidateName();
        }
    }

    /// <summary>
    /// Обновляет доступность сохранения после изменения ошибок строки
    /// </summary>
    public void OnEntryErrorsChanged() => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Останавливает отсчёт до скрытия окна
    /// </summary>
    public void Dispose() => Countdown.Dispose();

    [RelayCommand]
    private void Add()
    {
        var entry = new EntryViewModel(this, entry: null);
        Entries.Add(entry);
        SelectedEntry = entry;
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove()
    {
        var index = Entries.IndexOf(SelectedEntry!);
        Entries.RemoveAt(index);
        SelectedEntry = Entries.Count == 0 ? null : Entries[Math.Min(index, Entries.Count - 1)];
    }

    private bool CanRemove() => SelectedEntry is not null;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => MoveSelected(-1);

    private bool CanMoveUp() => SelectedEntry is not null && Entries.IndexOf(SelectedEntry) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => MoveSelected(+1);

    private bool CanMoveDown() =>
        SelectedEntry is not null && Entries.IndexOf(SelectedEntry) < Entries.Count - 1;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            _settings.Update(current => current with
            {
                Entries = new EntryList(
                    Entries.Select(entry => entry.ToEntry()),
                    GetSavedCurrentName(current.Entries.Current?.Name)),
                Notification = Notification,
                Protection = IsProtected ? ProtectionMode.Dpapi : ProtectionMode.None,
                StartupDisplayTime = TimeSpan.FromSeconds(decimal.ToInt32(StartupDisplaySeconds)),
                TrimWhitespace = TrimWhitespace,
            });

            if (IsAutostartEnabled != _wasAutostartEnabled)
            {
                _autostart.SetEnabled(IsAutostartEnabled);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
            or SecurityException)
        {
            ErrorMessage = $"Не удалось сохранить: {e.Message}";
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave() => Entries.All(entry => !entry.HasErrors);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    // Обрезка меняет то, что будет сохранено, а с ним — результат проверки
    partial void OnTrimWhitespaceChanged(bool value)
    {
        foreach (var entry in Entries)
        {
            entry.ValidateName();
            entry.ValidateValue();
        }
    }

    private void SelectNotification(NotificationKind kind, bool isSelected)
    {
        // Переключатель, с которого снимают отметку, ничего не выбирает: выбор делает
        // отмеченный
        if (isSelected)
        {
            Notification = kind;
        }
    }

    private void MoveSelected(int offset)
    {
        // Список может снять выделение при перемещении строки; оно восстанавливается
        var entry = SelectedEntry!;
        var index = Entries.IndexOf(entry);
        Entries.Move(index, index + offset);
        SelectedEntry = entry;
    }

    // Текущая запись определяется в момент сохранения: пока окно открыто, её могут
    // сменить из трея. Если её переименовали в окне, текущей остаётся она же
    private string? GetSavedCurrentName(string? currentName) =>
        Entries.FirstOrDefault(entry => entry.OriginalName is not null
            && entry.OriginalName == currentName)?.SavedName;

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Состав списка меняет совпадения имён, а позиция выделенной строки — доступность
        // перемещения. Значение новая строка проверила сама при создании
        ValidateNames();
        SaveCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
