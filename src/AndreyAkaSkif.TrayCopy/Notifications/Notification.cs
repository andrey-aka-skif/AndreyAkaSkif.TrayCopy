namespace AndreyAkaSkif.TrayCopy.Notifications;

/// <summary>
/// Представляет уведомление о действии в трее
/// </summary>
/// <param name="Title">Заголовок: что произошло</param>
/// <param name="Text">Текст: имя записи или причина сбоя</param>
internal sealed record Notification(string Title, string Text);
