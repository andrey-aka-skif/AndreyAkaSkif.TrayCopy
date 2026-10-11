namespace AndreyAkaSkif.TrayCopy.Tests;

/// <summary>
/// Предоставляет обработчик HTTP без сети: ответ строит переданная функция, запросы
/// запоминаются
/// </summary>
/// <param name="respond">Строит ответ на запрос</param>
internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    /// <summary>
    /// Полученные запросы в порядке поступления
    /// </summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return respond(request, cancellationToken);
    }
}
