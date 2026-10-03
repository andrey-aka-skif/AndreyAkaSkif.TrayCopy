using System.Reflection;

namespace AndreyAkaSkif.TrayCopy;

/// <summary>
/// Предоставляет сведения о приложении из атрибутов сборки
/// </summary>
internal static class AppInfo
{
    private static readonly Assembly AppAssembly = typeof(AppInfo).Assembly;

    /// <summary>
    /// Версия приложения: версия продукта без идентификатора коммита
    /// </summary>
    public static string Version { get; } = TrimSourceRevision(
        AppAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? string.Empty);

    /// <summary>
    /// Авторские права на приложение
    /// </summary>
    public static string Copyright { get; } =
        AppAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

    /// <summary>
    /// Адрес репозитория приложения
    /// </summary>
    public static Uri RepositoryUrl { get; } = new(AppAssembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryUrl").Value!);

    /// <summary>
    /// Путь к файлу лицензии приложения; файл лежит рядом с exe
    /// </summary>
    public static string LicensePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "LICENSE.txt");

    /// <summary>
    /// Путь к файлу с лицензиями сторонних компонентов; файл лежит рядом с exe
    /// </summary>
    public static string ThirdPartyNoticesPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");

    /// <summary>
    /// Отбрасывает идентификатор коммита, который SDK дописывает к версии продукта через «+»
    /// </summary>
    public static string TrimSourceRevision(string informationalVersion) =>
        informationalVersion.Split('+', 2)[0];
}
