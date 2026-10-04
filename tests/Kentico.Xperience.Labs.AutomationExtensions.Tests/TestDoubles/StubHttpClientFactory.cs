namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
