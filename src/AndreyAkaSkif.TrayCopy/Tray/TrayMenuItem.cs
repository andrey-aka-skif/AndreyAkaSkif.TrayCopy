namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Представляет пункт меню иконки в трее
/// </summary>
/// <param name="Text">Текст пункта</param>
/// <param name="Click">Действие по выбору пункта</param>
internal sealed record TrayMenuItem(string Text, Action Click)
{
    /// <summary>
    /// Жест, который выполняет то же действие без меню; меню показывает его справа от текста
    /// </summary>
    public string? Gesture { get; init; }

    /// <summary>
    /// Признак отмеченного пункта
    /// </summary>
    public bool IsChecked { get; init; }
}
