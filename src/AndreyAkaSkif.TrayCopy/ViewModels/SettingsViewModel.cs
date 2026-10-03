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

    // Удалённые строки, их позиции и признак текущей записи; последняя удалённая
    // возвращается первой
    private readonly Stack<(EntryViewModel Entry, int Index, bool WasCurrent)> _removed = new();

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

        CurrentEntry = FindByOriginalName(current.Entries.Current?.Name);
        Entries.CollectionChanged += OnEntriesChanged;
        settings.Changed += OnSettingsChanged;

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
    /// Строка текущей записи — той, что копирует клик по иконке в трее; сохраняется вместе
    /// со списком. <see langword="null"/> только у пустого списка
    /// </summary>
    [ObservableProperty]
    public partial EntryViewModel? CurrentEntry { get; set; }

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
    /// Сообщение о последней удалённой строке, которую можно вернуть; <see langword="null"/>,
    /// если возвращать нечего
    /// </summary>
    public string? RemovedEntryMessage =>
        _removed.TryPeek(out var removed)
            ? string.IsNullOrWhiteSpace(removed.Entry.Name)
                ? "Удалена запись без имени"
                : $"Удалена запись «{removed.Entry.Name}»"
            : null;

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
    /// Перестаёт следить за настройками и останавливает отсчёт до скрытия окна
    /// </summary>
    public void Dispose()
    {
        _settings.Changed -= OnSettingsChanged;
        Countdown.Dispose();
    }

    [RelayCommand]
    private void Add()
    {
        var entry = new EntryViewModel(this, entry: null);
        Entries.Add(entry);

        // Строка, добавленная в пустой список, — единственная, и текущей становится она
        CurrentEntry ??= entry;
    }

    [RelayCommand]
    private void Remove(EntryViewModel entry)
    {
        var index = Entries.IndexOf(entry);
        var wasCurrent = entry == CurrentEntry;
        Entries.RemoveAt(index);

        // Как и в списке, сохранённом без текущей записи, текущей становится первая
        if (wasCurrent)
        {
            CurrentEntry = Entries.FirstOrDefault();
        }

        // Пустую строку возвращать незачем
        if (entry.Name.Length > 0 || entry.Value.Length > 0)
        {
            _removed.Push((entry, index, wasCurrent));
            OnRemovedChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndoRemove))]
    private void UndoRemove()
    {
        // Возвращается та же строка: по её исходному имени окно узнаёт её, когда текущую
        // запись меняют из трея. Пока строки не было в списке, могла переключиться обрезка,
        // поэтому значение проверяется заново; имена перепроверяет изменение списка
        var (entry, index, wasCurrent) = _removed.Pop();
        Entries.Insert(Math.Min(index, Entries.Count), entry);
        entry.ValidateValue();

        // Отметка возвращается вместе со строкой. В пустой список возвращается строка, которая
        // была в нём последней, то есть текущей, — список снова получает текущую запись
        if (wasCurrent)
        {
            CurrentEntry = entry;
        }

        OnRemovedChanged();
    }

    private bool CanUndoRemove() => _removed.Count > 0;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp(EntryViewModel entry) => Move(entry, -1);

    // Пока привязка не задала параметр, команда проверяется с null: такой строки в списке нет
    private bool CanMoveUp(EntryViewModel entry) => Entries.IndexOf(entry) > 0;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown(EntryViewModel entry) => Move(entry, +1);

    private bool CanMoveDown(EntryViewModel entry)
    {
        var index = Entries.IndexOf(entry);
        return index >= 0 && index < Entries.Count - 1;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            _settings.Update(current => current with
            {
                Entries = new EntryList(
                    Entries.Select(entry => entry.ToEntry()), CurrentEntry?.SavedName),
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

    partial void OnCurrentEntryChanged(EntryViewModel? oldValue, EntryViewModel? newValue)
    {
        oldValue?.NotifyIsCurrentChanged();
        newValue?.NotifyIsCurrentChanged();
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

    private void Move(EntryViewModel entry, int offset)
    {
        var index = Entries.IndexOf(entry);
        Entries.Move(index, index + offset);
    }

    private void OnRemovedChanged()
    {
        OnPropertyChanged(nameof(RemovedEntryMessage));
        UndoRemoveCommand.NotifyCanExecuteChanged();
    }

    // Строка ищется по имени, под которым запись сохранена: переименование в окне её не
    // теряет
    private EntryViewModel? FindByOriginalName(string? name) =>
        Entries.FirstOrDefault(entry => entry.OriginalName is not null
            && entry.OriginalName == name);

    // Пока окно открыто, текущую запись могут сменить из трея: отметка переходит на ту же
    // запись, если она есть в окне. Действует последний выбор — в трее или в окне
    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (FindByOriginalName(_settings.Current.Entries.Current?.Name) is { } entry)
        {
            CurrentEntry = entry;
        }
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Состав списка меняет совпадения имён, а позиции строк — доступность перемещения.
        // Значение новая строка проверила сама при создании
        ValidateNames();
        SaveCommand.NotifyCanExecuteChanged();
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
