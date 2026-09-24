namespace AndreyAkaSkif.TrayCopy.Autostart.RunKey;

/// <summary>
/// Представляет параметры автозапуска <see cref="RunKeyAutostart"/>
/// </summary>
internal sealed class RunKeyAutostartOptions
{
    /// <summary>
    /// Путь к ключу Run относительно HKEY_CURRENT_USER
    /// </summary>
    public string RunKeyPath { get; set; } = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Путь относительно HKEY_CURRENT_USER к ключу, в котором «Автозагрузка» Диспетчера
    /// задач отмечает отключённый запуск
    /// </summary>
    public string ApprovalKeyPath { get; set; } =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <summary>
    /// Имя значения в ключе Run и в ключе отметок
    /// </summary>
    public string ValueName { get; set; } = "AndreyAkaSkif.TrayCopy";

    /// <summary>
    /// Путь к исполняемому файлу, который запускается при входе; по умолчанию — файл
    /// текущего процесса
    /// </summary>
    public string ExecutablePath { get; set; } = Environment.ProcessPath ?? string.Empty;
}
