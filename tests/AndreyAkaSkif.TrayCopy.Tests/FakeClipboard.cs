namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет буфер обмена в памяти: запоминает помещённые тексты или бросает заданное
/// исключение
/// </summary>
internal sealed class FakeClipboard : IClipboard
{
    /// <summary>
    /// Тексты в порядке помещения в буфер
    /// </summary>
    public List<string> Texts { get; } = [];

    /// <summary>
    /// Исключение, которое бросает <see cref="SetText"/>; <see langword="null"/> — текст
    /// помещается
    /// </summary>
    public Exception? Exception { get; set; }

    public void SetText(string text)
    {
        if (Exception is not null)
        {
            throw Exception;
        }

        Texts.Add(text);
    }
}
