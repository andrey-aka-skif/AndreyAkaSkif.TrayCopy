using System.Reflection;

namespace AndreyAkaSkif.TrayCopy;

/// <summary>
/// Предоставляет сведения о приложении из атрибутов сборки
/// </summary>
internal static class AppInfo
{
    /// <summary>
    /// Версия приложения: версия продукта без идентификатора коммита
    /// </summary>
    public static string Version { get; } = TrimSourceRevision(
        typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? string.Empty);

    /// <summary>
    /// Отбрасывает идентификатор коммита, который SDK дописывает к версии продукта через «+»
    /// </summary>
    public static string TrimSourceRevision(string informationalVersion) =>
        informationalVersion.Split('+', 2)[0];
}
