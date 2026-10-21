using System.Net;
using System.Text.Json;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class WebhookSenderTests
{
    private const string ENDPOINT_NAME = "crm";
    private const string ENDPOINT_URL = "https://example.test/hooks/crm";


    [Test]
    public async Task Send_SuccessStatus_ReturnsSucceeded()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.Accepted);

        var result = await CreateSender(handler, CreateEndpoint()).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("HTTP 202"));
            Assert.That(handler.Requests.Single().RequestUri, Is.EqualTo(new Uri(ENDPOINT_URL)));
            Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Post));
        });
    }


    [Test]
    public async Task Send_ErrorStatus_ReturnsFailed()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.InternalServerError);

        var result = await CreateSender(handler, CreateEndpoint()).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("HTTP 500"));
        });
    }


    [Test]
    public async Task Send_UnknownEndpoint_ReturnsFailedWithoutRequest()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        var result = await CreateSender(handler, CreateEndpoint()).Send("unknown", CreatePayload(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(handler.Requests, Is.Empty);
            Assert.That(result.Message, Does.Contain("unknown").And.Contain("AutomationExtensions:Webhooks:Endpoints"));
        });
    }


    [Test]
    public async Task Send_RequestThrows_ReturnsFailed()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused"));

        var result = await CreateSender(handler, CreateEndpoint()).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Exception, Is.InstanceOf<HttpRequestException>());
        });
    }


    [Test]
    public async Task Send_EndpointTimesOut_ReturnsFailed()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var endpoint = CreateEndpoint();
        endpoint.TimeoutSeconds = 1;

        var result = await CreateSender(handler, endpoint).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("Timed out after 1 s"));
            Assert.That(result.Exception, Is.InstanceOf<OperationCanceledException>());
        });
    }


    [Test]
    public async Task Send_SecretConfigured_AddsSignatureOfBody()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);
        var endpoint = CreateEndpoint();
        endpoint.Secret = "top-secret";

        await CreateSender(handler, endpoint).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        string signature = handler.Requests.Single().Headers.GetValues(WebhookEndpointOptions.DEFAULT_SIGNATURE_HEADER_NAME).Single();
        Assert.That(signature, Is.EqualTo(WebhookSignature.Compute("top-secret", handler.RequestBodies.Single())));
    }


    [Test]
    public async Task Send_NoSecret_OmitsSignature()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        await CreateSender(handler, CreateEndpoint()).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        Assert.That(handler.Requests.Single().Headers.Contains(WebhookEndpointOptions.DEFAULT_SIGNATURE_HEADER_NAME), Is.False);
    }


    [Test]
    public async Task Send_Payload_SerializedAsCamelCaseJson()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        await CreateSender(handler, CreateEndpoint()).Send(ENDPOINT_NAME, CreatePayload(), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBodies.Single());
        var root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("eventName").GetString(), Is.EqualTo("lead-qualified"));
            Assert.That(root.GetProperty("processName").GetString(), Is.EqualTo("Lead nurture"));
            Assert.That(root.GetProperty("contact").GetProperty("guid").GetGuid(), Is.EqualTo(Guid.Parse("8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10")));
        });
    }


    private static WebhookSender CreateSender(HttpMessageHandler handler, WebhookEndpointOptions endpoint)
    {
        var options = new AutomationExtensionsOptions();
        options.Webhooks.Endpoints[ENDPOINT_NAME] = endpoint;

        return new WebhookSender(
            new StubHttpClientFactory(handler),
            new TestOptionsMonitor<AutomationExtensionsOptions>(options),
            NullLoggerFactory.Instance);
    }


    private static WebhookEndpointOptions CreateEndpoint() => new() { Url = ENDPOINT_URL };


    private static WebhookPayload CreatePayload() => new()
    {
        EventName = "lead-qualified",
        OccurredAt = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero),
        ProcessName = "Lead nurture",
        TriggerIdentifier = "Labs.AutomationExtensions.ContactDateAnniversary",
        Contact = new WebhookContact { Guid = Guid.Parse("8f6c2b8e-6d33-4c4a-9c47-0e2b1d1f6a10") }
    };
}
