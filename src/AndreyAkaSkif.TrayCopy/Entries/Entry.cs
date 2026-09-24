using System.Text;

namespace AndreyAkaSkif.TrayCopy.Entries;

/// <summary>
/// Представляет запись: строку для копирования и имя, под которым она показывается
/// </summary>
internal sealed record Entry
{
    /// <summary>
    /// Создаёт запись
    /// </summary>
    /// <param name="name">Имя записи</param>
    /// <param name="value">Строка, которая копируется в буфер обмена</param>
    /// <exception cref="ArgumentException">
    /// Имя или значение нарушает правило из <see cref="EntryRules"/>
    /// </exception>
    public Entry(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Имя записи
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Имя нарушает правило из <see cref="EntryRules"/>
    /// </exception>
    public string Name
    {
        get;
        init => field = Validated(value, EntryRules.CheckName(value), nameof(Name));
    }

    /// <summary>
    /// Строка, которая копируется в буфер обмена
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Значение нарушает правило из <see cref="EntryRules"/>
    /// </exception>
    public string Value
    {
        get;
        init => field = Validated(value, EntryRules.CheckValue(value), nameof(Value));
    }

    // Недопустимые данные сюда не доходят: окно настроек проверяет их по тем же правилам,
    // а хранилище считает файл с ними нечитаемым. Исключение — крайняя мера
    private static string Validated(string text, string? error, string paramName) =>
        error is null ? text : throw new ArgumentException(error, paramName);

    // Значение — секрет (токен), поэтому в ToString, а с ним в логи, сообщения об ошибках
    // и окно отладчика попадает только имя
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ").Append(Name);
        return true;
    }
}
