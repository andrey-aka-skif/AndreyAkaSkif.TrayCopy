namespace AndreyAkaSkif.TrayCopy.Settings;

/// <summary>
/// Определяет, в каком виде хранилище сохраняет значения записей
/// </summary>
internal enum ProtectionMode
{
    /// <summary>
    /// Шифрование DPAPI в области текущего пользователя
    /// </summary>
    Dpapi,

    /// <summary>
    /// Открытый текст
    /// </summary>
    None,
}
