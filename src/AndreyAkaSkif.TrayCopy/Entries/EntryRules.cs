namespace AndreyAkaSkif.TrayCopy.Entries;

/// <summary>
/// Определяет правила записей — единственное место, где они заданы. Проверками пользуются
/// и модель, отклоняя недопустимые данные, и окно настроек, показывая ошибки ввода
/// </summary>
internal static class EntryRules
{
    /// <summary>
    /// Наибольшая длина имени записи
    /// </summary>
    public const int MaxNameLength = 64;

    // Правило — условие нарушения и сообщение о нём. Проверка возвращает сообщение первого
    // нарушенного правила, поэтому порядок важен: от общего к частному
    private static readonly Rule[] NameRules =
    [
        new(string.IsNullOrWhiteSpace, "Укажите имя"),
        new(HasControlCharacters, "Имя должно быть одной строкой"),
        new(name => name.Length > MaxNameLength, $"Имя длиннее {MaxNameLength} символов"),
    ];

    private static readonly Rule[] ValueRules =
    [
        new(string.IsNullOrEmpty, "Укажите значение"),
        new(HasControlCharacters, "Значение должно быть одной строкой"),
    ];

    /// <summary>
    /// Сравнение имён при проверке уникальности: без учёта регистра
    /// </summary>
    public static StringComparer NameComparer => StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Проверяет имя записи; возвращает сообщение о нарушенном правиле или
    /// <see langword="null"/>, если имя допустимо
    /// </summary>
    public static string? CheckName(string name) => Check(NameRules, name);

    /// <summary>
    /// Проверяет значение записи; возвращает сообщение о нарушенном правиле или
    /// <see langword="null"/>, если значение допустимо
    /// </summary>
    public static string? CheckValue(string value) => Check(ValueRules, value);

    /// <summary>
    /// Проверяет, что имени <paramref name="name"/> нет среди имён остальных записей
    /// <paramref name="otherNames"/>; возвращает сообщение о нарушении или
    /// <see langword="null"/>
    /// </summary>
    public static string? CheckUniqueName(string name, IEnumerable<string> otherNames) =>
        otherNames.Contains(name, NameComparer) ? "Запись с таким именем уже есть" : null;

    private static bool HasControlCharacters(string text) => text.Any(char.IsControl);

    private static string? Check(Rule[] rules, string text) =>
        Array.Find(rules, rule => rule.IsViolatedBy(text))?.Message;

    private sealed record Rule(Func<string, bool> IsViolatedBy, string Message);
}
