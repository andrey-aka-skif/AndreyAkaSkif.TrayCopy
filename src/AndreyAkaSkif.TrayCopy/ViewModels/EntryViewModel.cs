using System.Collections;
using System.ComponentModel;
using AndreyAkaSkif.TrayCopy.Entries;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AndreyAkaSkif.TrayCopy.ViewModels;

/// <summary>
/// Представляет строку списка записей в окне настроек: редактируемые имя и значение с
/// проверкой по правилам <see cref="EntryRules"/>
/// </summary>
/// <remarks>
/// Ошибки отдаются через <see cref="INotifyDataErrorInfo"/>, и Avalonia показывает их под
/// полем. Проверяется то, что будет сохранено: имя и значение после обрезки пробелов, если
/// она включена
/// </remarks>
internal sealed partial class EntryViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly SettingsViewModel _owner;
    private readonly Dictionary<string, string> _errors = [];

    /// <summary>
    /// Создаёт строку для записи <paramref name="entry"/> или пустую строку, если записи нет
    /// </summary>
    /// <param name="owner">Окно, в списке которого находится строка</param>
    /// <param name="entry">Сохранённая запись; <see langword="null"/> у новой строки</param>
    public EntryViewModel(SettingsViewModel owner, Entry? entry)
    {
        _owner = owner;
        OriginalName = entry?.Name;
        Name = entry?.Name ?? string.Empty;
        Value = entry?.Value ?? string.Empty;
    }

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Имя записи, из которой создана строка; <see langword="null"/> у новой строки
    /// </summary>
    public string? OriginalName { get; }

    /// <summary>
    /// Имя в том виде, в каком его ввёл пользователь
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>
    /// Значение в том виде, в каком его ввёл пользователь
    /// </summary>
    [ObservableProperty]
    public partial string Value { get; set; }

    /// <summary>
    /// Имя в том виде, в каком оно будет сохранено
    /// </summary>
    public string SavedName => _owner.TrimWhitespace ? Name.Trim() : Name;

    /// <summary>
    /// Значение в том виде, в каком оно будет сохранено
    /// </summary>
    public string SavedValue => _owner.TrimWhitespace ? Value.Trim() : Value;

    /// <inheritdoc/>
    public bool HasErrors => _errors.Count > 0;

    /// <inheritdoc/>
    public IEnumerable GetErrors(string? propertyName) =>
        propertyName is not null && _errors.TryGetValue(propertyName, out var error)
            ? new[] { error }
            : Array.Empty<string>();

    /// <summary>
    /// Проверяет имя, в том числе на совпадение с именами остальных строк списка
    /// </summary>
    public void ValidateName() => SetError(
        nameof(Name),
        EntryRules.CheckName(SavedName)
            ?? EntryRules.CheckUniqueName(SavedName, _owner.GetSavedNamesExcept(this)));

    /// <summary>
    /// Проверяет значение
    /// </summary>
    public void ValidateValue() => SetError(nameof(Value), EntryRules.CheckValue(SavedValue));

    /// <summary>
    /// Возвращает запись в том виде, в каком она будет сохранена
    /// </summary>
    public Entry ToEntry() => new(SavedName, SavedValue);

    // Имя проверяет владелец: изменение одного имени может снять или создать совпадение
    // у других строк
    partial void OnNameChanged(string value) => _owner.ValidateNames();

    partial void OnValueChanged(string value) => ValidateValue();

    private void SetError(string propertyName, string? error)
    {
        if (_errors.GetValueOrDefault(propertyName) == error)
        {
            return;
        }

        if (error is null)
        {
            _errors.Remove(propertyName);
        }
        else
        {
            _errors[propertyName] = error;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        _owner.OnEntryErrorsChanged();
    }
}
