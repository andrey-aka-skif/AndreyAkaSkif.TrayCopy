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
    /// Код виртуальной клавиши Shift для <see cref="GetAsyncKeyState"/>
    /// </summary>
    public const int VkShift = 0x10;

    /// <summary>
    /// Признак «любой процесс» для <see cref="AllowSetForegroundWindow"/>
    /// </summary>
    public const uint AsfwAny = uint.MaxValue;

    /// <summary>
    /// Стандартный формат буфера обмена «текст в UTF-16 с завершающим нулём»
    /// </summary>
    public const uint CfUnicodeText = 13;

    /// <summary>
    /// Флаги <see cref="GlobalAlloc"/>: перемещаемый блок, заполненный нулями, — такой
    /// требует <see cref="SetClipboardData"/>
    /// </summary>
    public const uint GHnd = 0x0042;

    /// <summary>
    /// Родитель окна сообщений (message-only) для <see cref="CreateWindowEx"/>
    /// </summary>
    public const nint HwndMessage = -3;

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

    /// <summary>
    /// Возвращает состояние клавиши в момент вызова; старший бит установлен, если клавиша
    /// нажата
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial short GetAsyncKeyState(int key);

    /// <summary>
    /// Разрешает процессу <paramref name="processId"/> вывести своё окно на передний план;
    /// вызывающий процесс должен сам иметь это право
    /// </summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllowSetForegroundWindow(uint processId);

    /// <summary>
    /// Создаёт окно
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(
        uint exStyle,
        string className,
        string? windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint param);

    /// <summary>
    /// Уничтожает окно, созданное вызывающим потоком
    /// </summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hwnd);

    /// <summary>
    /// Открывает буфер обмена; <paramref name="hwnd"/> станет владельцем буфера после
    /// <see cref="EmptyClipboard"/>
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(nint hwnd);

    /// <summary>
    /// Закрывает буфер обмена
    /// </summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    /// <summary>
    /// Очищает буфер обмена и делает его владельцем окно, открывшее буфер
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    /// <summary>
    /// Помещает в буфер обмена данные в заданном формате; при успехе блок памяти переходит
    /// во владение системы
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetClipboardData(uint format, nint memory);

    /// <summary>
    /// Возвращает номер формата буфера обмена по его имени, регистрируя формат при первом
    /// обращении
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint RegisterClipboardFormat(string name);

    /// <summary>
    /// Выделяет блок глобальной памяти
    /// </summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalAlloc(uint flags, nuint bytes);

    /// <summary>
    /// Закрепляет блок глобальной памяти и возвращает указатель на его начало
    /// </summary>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalLock(nint memory);

    /// <summary>
    /// Снимает закрепление блока глобальной памяти
    /// </summary>
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(nint memory);

    /// <summary>
    /// Освобождает блок глобальной памяти
    /// </summary>
    [LibraryImport("kernel32.dll")]
    public static partial nint GlobalFree(nint memory);
}
