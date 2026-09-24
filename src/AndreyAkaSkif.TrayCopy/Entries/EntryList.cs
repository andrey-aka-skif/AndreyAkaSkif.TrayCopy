namespace AndreyAkaSkif.TrayCopy.Entries;

/// <summary>
/// Представляет неизменяемый упорядоченный список записей с текущей записью. Имена записей
/// уникальны по правилу <see cref="EntryRules.CheckUniqueName"/>. Текущая запись всегда
/// принадлежит списку; её нет только у пустого списка
/// </summary>
internal sealed class EntryList
{
    // Индекс текущей записи; -1 у пустого списка
    private readonly int _currentIndex;

    /// <summary>
    /// Создаёт список из копии <paramref name="items"/>. Текущей становится запись с именем
    /// <paramref name="currentName"/>, а если такой нет — первая
    /// </summary>
    /// <exception cref="ArgumentException">Имена записей повторяются</exception>
    public EntryList(IEnumerable<Entry> items, string? currentName)
    {
        Entry[] copy = [.. items];
        for (var i = 1; i < copy.Length; i++)
        {
            // Крайняя мера, как и в Entry: окно настроек не даёт сохранить повторы
            var error = EntryRules.CheckUniqueName(
                copy[i].Name, copy.Take(i).Select(entry => entry.Name));
            if (error is not null)
            {
                throw new ArgumentException($"{error}: {copy[i].Name}", nameof(items));
            }
        }

        Items = copy;
        _currentIndex = copy.Length == 0
            ? -1
            : Math.Max(0, Array.FindIndex(copy, entry => entry.Name == currentName));
    }

    private EntryList(IReadOnlyList<Entry> items, int currentIndex)
    {
        Items = items;
        _currentIndex = currentIndex;
    }

    /// <summary>
    /// Пустой список
    /// </summary>
    public static EntryList Empty { get; } = new([], currentName: null);

    /// <summary>
    /// Записи в порядке переключения
    /// </summary>
    public IReadOnlyList<Entry> Items { get; }

    /// <summary>
    /// Текущая запись; <see langword="null"/> у пустого списка
    /// </summary>
    public Entry? Current => _currentIndex < 0 ? null : Items[_currentIndex];

    /// <summary>
    /// Возвращает список с теми же записями, в котором текущей стала следующая запись; после
    /// последней — первая
    /// </summary>
    public EntryList SelectNext() =>
        Items.Count == 0 ? this : new EntryList(Items, (_currentIndex + 1) % Items.Count);
}
