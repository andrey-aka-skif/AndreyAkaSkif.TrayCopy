using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace AndreyAkaSkif.TrayCopy.Autostart.RunKey;

/// <summary>
/// Управляет запуском приложения при входе в Windows через значение в ключе Run реестра
/// текущего пользователя
/// </summary>
/// <remarks>
/// «Автозагрузка» Диспетчера задач отключает запуск, не удаляя значение из Run: она пишет
/// одноимённое значение-отметку в ключ StartupApproved\Run. Формат отметки не
/// документирован: 12 байт, первый — состояние (02 — включено, 03 — отключено), дальше —
/// время отключения. Отключённым считается запуск с нечётным первым байтом
/// </remarks>
/// <param name="options">Параметры: ключи реестра, имя значения, путь к исполняемому файлу</param>
internal sealed class RunKeyAutostart(IOptions<RunKeyAutostartOptions> options) : IAutostart
{
    private readonly RunKeyAutostartOptions _options = options.Value;

    // Путь в кавычках: в нём могут быть пробелы
    private string Command => $"\"{_options.ExecutablePath}\"";

    /// <summary>
    /// Признак того, что эта копия приложения запустится при входе в Windows: в ключе Run
    /// записан её путь, и Диспетчер задач запуск не отключил. Значение с путём другой копии
    /// (например, установленной, если спрашивает отладочная сборка) своим не считается
    /// </summary>
    public bool IsEnabled => IsOwnCommandRegistered() && !IsDisabledByTaskManager();

    /// <summary>
    /// Включает запуск: записывает путь этой копии в ключ Run и снимает отметку об отключении.
    /// Выключает — удаляет значение и отметку, если значение записано этой копией; значение
    /// другой копии не трогает
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(_options.RunKeyPath);
            runKey.SetValue(_options.ValueName, Command, RegistryValueKind.String);
        }
        else
        {
            if (!IsOwnCommandRegistered())
            {
                return;
            }

            using var runKey = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath, writable: true);
            runKey?.DeleteValue(_options.ValueName, throwOnMissingValue: false);
        }

        // При включении отметка отменила бы запуск, при выключении осталась бы мусором
        using var approvalKey = Registry.CurrentUser.OpenSubKey(
            _options.ApprovalKeyPath, writable: true);
        approvalKey?.DeleteValue(_options.ValueName, throwOnMissingValue: false);
    }

    private bool IsOwnCommandRegistered()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(_options.RunKeyPath);
        return runKey?.GetValue(_options.ValueName) is string command
            && string.Equals(command, Command, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDisabledByTaskManager()
    {
        using var approvalKey = Registry.CurrentUser.OpenSubKey(_options.ApprovalKeyPath);
        return approvalKey?.GetValue(_options.ValueName) is byte[] { Length: > 0 } approval
            && (approval[0] & 1) == 1;
    }
}
