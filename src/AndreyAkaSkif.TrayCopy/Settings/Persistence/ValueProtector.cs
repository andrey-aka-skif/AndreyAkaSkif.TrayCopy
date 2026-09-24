using System.Security.Cryptography;
using System.Text;

namespace AndreyAkaSkif.TrayCopy.Settings.Persistence;

/// <summary>
/// Переводит значения записей в вид для файла настроек и обратно
/// </summary>
internal static class ValueProtector
{
    // Дополнительный секрет DPAPI: без него значение расшифрует любое приложение текущего
    // пользователя. При смене уже сохранённые значения перестанут расшифровываться
    private static readonly byte[] Entropy = "AndreyAkaSkif.TrayCopy"u8.ToArray();

    /// <summary>
    /// Возвращает значение в том виде, в котором оно хранится в файле при режиме
    /// <paramref name="mode"/>: шифр DPAPI в base64 или открытый текст
    /// </summary>
    public static string Protect(string value, ProtectionMode mode) => mode switch
    {
        ProtectionMode.Dpapi => Convert.ToBase64String(ProtectedData.Protect(
            Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser)),
        ProtectionMode.None => value,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    /// <summary>
    /// Восстанавливает значение из вида, в котором оно хранится в файле при режиме
    /// <paramref name="mode"/>
    /// </summary>
    /// <exception cref="FormatException">Хранимое значение — не base64</exception>
    /// <exception cref="CryptographicException">
    /// Значение зашифровано другим пользователем, на другом компьютере или повреждено
    /// </exception>
    public static string Unprotect(string stored, ProtectionMode mode) => mode switch
    {
        ProtectionMode.Dpapi => Encoding.UTF8.GetString(ProtectedData.Unprotect(
            Convert.FromBase64String(stored), Entropy, DataProtectionScope.CurrentUser)),
        ProtectionMode.None => stored,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
}
