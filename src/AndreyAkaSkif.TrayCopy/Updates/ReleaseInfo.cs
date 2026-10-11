namespace AndreyAkaSkif.TrayCopy.Updates;

/// <summary>
/// Представляет выпуск приложения
/// </summary>
/// <param name="Version">Версия выпуска</param>
/// <param name="PageUrl">Адрес страницы выпуска: описание изменений и установщики</param>
internal sealed record ReleaseInfo(ReleaseVersion Version, Uri PageUrl);
