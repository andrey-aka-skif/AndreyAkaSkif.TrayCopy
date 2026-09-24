namespace AndreyAkaSkif.TrayCopy.Clipboard;

/// <summary>
/// Определяет, как текст помещается в буфер обмена
/// </summary>
internal interface IClipboard
{
    /// <summary>
    /// Помещает <paramref name="text"/> в буфер обмена так, чтобы он не попал в историю
    /// буфера и не ушёл на другие устройства пользователя
    /// </summary>
    /// <exception cref="IOException">Не удалось поместить текст в буфер обмена</exception>
    void SetText(string text);
}
