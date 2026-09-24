using System.ComponentModel;
using System.Runtime.InteropServices;
using AndreyAkaSkif.TrayCopy.Interop;

namespace AndreyAkaSkif.TrayCopy.Clipboard.WinApi;

/// <summary>
/// Помещает текст в буфер обмена через Win32 API вместе с форматами, по которым его
/// пропускают история буфера Windows (Win+V), облачный буфер и сторонние менеджеры буфера
/// </summary>
/// <remarks>
/// Владелец буфера — собственное скрытое окно сообщений: без окна-владельца
/// <c>SetClipboardData</c> отказывает. Окно создаётся при первом копировании в вызывающем
/// потоке, и этот поток должен обрабатывать сообщения: владельцу буфера их посылают другие
/// программы. В приложении это поток интерфейса
/// </remarks>
internal sealed class Win32Clipboard : IClipboard, IDisposable
{
    // Буфер может держать открытым другая программа. Попытки идут в потоке интерфейса,
    // поэтому ожидание ограничено 200 мс
    private const int OpenAttempts = 10;
    private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(20);

    // Зарегистрированные форматы, по которым содержимое буфера пропускают: первые три
    // обрабатывает Windows (Microsoft Learn, «Cloud Clipboard and Clipboard History
    // Formats»), последний — сторонние менеджеры буфера. Двум форматам Can… нужен DWORD 0,
    // остальным подходят любые данные, поэтому у всех — DWORD 0
    private static readonly string[] ExclusionFormatNames =
    [
        "ExcludeClipboardContentFromMonitorProcessing",
        "CanIncludeInClipboardHistory",
        "CanUploadToCloudClipboard",
        "Clipboard Viewer Ignore",
    ];

    private nint _window;
    private uint[]? _exclusionFormats;

    /// <summary>
    /// Помещает <paramref name="text"/> в буфер обмена вместе с форматами исключения из
    /// истории и облачного буфера
    /// </summary>
    /// <exception cref="IOException">
    /// Буфер обмена занят другой программой или не принял данные
    /// </exception>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var window = EnsureWindow();
        var exclusionFormats = _exclusionFormats ??= RegisterExclusionFormats();

        Open(window);
        try
        {
            if (!NativeMethods.EmptyClipboard())
            {
                throw Failure();
            }

            SetData(NativeMethods.CfUnicodeText, MemoryMarshal.AsBytes(text.AsSpan()),
                terminatorBytes: sizeof(char));

            ReadOnlySpan<byte> zero = [0, 0, 0, 0];
            foreach (var format in exclusionFormats)
            {
                SetData(format, zero, terminatorBytes: 0);
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Уничтожает окно-владелец буфера; помещённый в буфер текст остаётся
    /// </summary>
    public void Dispose()
    {
        if (_window != 0)
        {
            NativeMethods.DestroyWindow(_window);
            _window = 0;
        }
    }

    // Окно сообщений системного класса STATIC: своя оконная процедура не нужна, сообщения
    // владельцу буфера без отложенной выдачи данных обрабатываются по умолчанию
    private nint EnsureWindow()
    {
        if (_window == 0)
        {
            _window = NativeMethods.CreateWindowEx(
                exStyle: 0, "STATIC", windowName: null, style: 0, x: 0, y: 0, width: 0,
                height: 0, NativeMethods.HwndMessage, menu: 0, instance: 0, param: 0);
            if (_window == 0)
            {
                throw Failure();
            }
        }

        return _window;
    }

    private static uint[] RegisterExclusionFormats() =>
    [
        .. ExclusionFormatNames.Select(name =>
        {
            var format = NativeMethods.RegisterClipboardFormat(name);
            return format != 0 ? format : throw Failure();
        }),
    ];

    private static void Open(nint window)
    {
        for (var attempt = 1; ; attempt++)
        {
            if (NativeMethods.OpenClipboard(window))
            {
                return;
            }

            if (attempt == OpenAttempts)
            {
                throw new IOException(
                    "Буфер обмена занят другой программой",
                    new Win32Exception(Marshal.GetLastPInvokeError()));
            }

            Thread.Sleep(OpenRetryDelay);
        }
    }

    // Блок памяти заполнен нулями при выделении, поэтому завершающий ноль — это
    // terminatorBytes лишних байтов после данных
    private static unsafe void SetData(uint format, ReadOnlySpan<byte> data, int terminatorBytes)
    {
        var size = data.Length + terminatorBytes;
        var memory = NativeMethods.GlobalAlloc(NativeMethods.GHnd, (nuint)size);
        if (memory == 0)
        {
            throw Failure();
        }

        var pointer = NativeMethods.GlobalLock(memory);
        if (pointer == 0)
        {
            var error = Failure();
            NativeMethods.GlobalFree(memory);
            throw error;
        }

        data.CopyTo(new Span<byte>((void*)pointer, size));
        NativeMethods.GlobalUnlock(memory);

        // При успехе памятью владеет система, при отказе — по-прежнему вызывающий
        if (NativeMethods.SetClipboardData(format, memory) == 0)
        {
            var error = Failure();
            NativeMethods.GlobalFree(memory);
            throw error;
        }
    }

    private static IOException Failure() =>
        new("Не удалось поместить текст в буфер обмена",
            new Win32Exception(Marshal.GetLastPInvokeError()));
}
