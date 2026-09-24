using System.Runtime.InteropServices;

namespace AndreyAkaSkif.TrayCopy.Interop;

/// <summary>
/// Предоставляет вызовы Win32 API, которых нет в .NET и используемых библиотеках
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// Индекс метрики «ширина маленькой иконки» для <see cref="GetSystemMetricsForDpi"/>
    /// </summary>
    public const int SmCxSmIcon = 49;

    /// <summary>
    /// Возвращает DPI монитора, на котором находится окно
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial uint GetDpiForWindow(nint hwnd);

    /// <summary>
    /// Возвращает системную метрику, пересчитанную под заданный DPI
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetricsForDpi(int index, uint dpi);
}
