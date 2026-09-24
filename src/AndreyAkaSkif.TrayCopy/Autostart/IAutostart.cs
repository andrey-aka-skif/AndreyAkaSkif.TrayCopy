namespace AndreyAkaSkif.TrayCopy.Autostart;

/// <summary>
/// Определяет, как приложение запускается при входе пользователя в Windows
/// </summary>
internal interface IAutostart
{
    /// <summary>
    /// Признак того, что эта копия приложения запустится при входе в Windows. Отражает
    /// фактическое состояние, а не сохранённую настройку
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Включает или выключает запуск этой копии приложения при входе в Windows
    /// </summary>
    void SetEnabled(bool enabled);
}
