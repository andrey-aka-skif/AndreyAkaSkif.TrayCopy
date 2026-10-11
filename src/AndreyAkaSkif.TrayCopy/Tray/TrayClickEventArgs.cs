namespace AndreyAkaSkif.TrayCopy.Tray;

/// <summary>
/// Предоставляет данные о клике по иконке в трее
/// </summary>
/// <param name="button">Кнопка мыши</param>
/// <param name="isShiftPressed">Признак нажатого Shift</param>
/// <param name="isControlPressed">Признак нажатого Ctrl</param>
internal sealed class TrayClickEventArgs(
    TrayButton button, bool isShiftPressed, bool isControlPressed) : EventArgs
{
    /// <summary>
    /// Кнопка мыши
    /// </summary>
    public TrayButton Button { get; } = button;

    /// <summary>
    /// Признак нажатого Shift в момент клика
    /// </summary>
    public bool IsShiftPressed { get; } = isShiftPressed;

    /// <summary>
    /// Признак нажатого Ctrl в момент клика
    /// </summary>
    public bool IsControlPressed { get; } = isControlPressed;
}
