using System.Net;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.TestDoubles;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];


    public List<string> RequestBodies { get; } = [];


    public static StubHttpMessageHandler Returning(HttpStatusCode statusCode) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        return await responder(request, cancellationToken);
    }
}
