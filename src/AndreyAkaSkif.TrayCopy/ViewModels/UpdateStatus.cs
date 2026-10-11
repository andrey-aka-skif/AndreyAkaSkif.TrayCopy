namespace AndreyAkaSkif.TrayCopy.ViewModels;

/// <summary>
/// Определяет состояние проверки обновлений в окне «О программе»
/// </summary>
internal enum UpdateStatus
{
    /// <summary>
    /// Проверка идёт
    /// </summary>
    Checking,

    /// <summary>
    /// Установлена последняя версия
    /// </summary>
    UpToDate,

    /// <summary>
    /// Вышла версия новее установленной
    /// </summary>
    UpdateAvailable,

    /// <summary>
    /// Проверка не удалась
    /// </summary>
    Failed,
}
