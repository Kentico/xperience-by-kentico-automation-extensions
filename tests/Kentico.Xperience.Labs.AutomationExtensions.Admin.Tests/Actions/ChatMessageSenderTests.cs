using System.Net;
using System.Text.Json;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ChatMessageSenderTests
{
    private const string CHANNEL_NAME = "sales";
    private const string CHANNEL_URL = "https://example.test/hooks/secret-token-123";


    [TestCase(ChatProvider.Slack, HttpStatusCode.OK)]
    [TestCase(ChatProvider.Teams, HttpStatusCode.Accepted)]
    public async Task Send_SuccessStatus_ReturnsSucceeded(ChatProvider provider, HttpStatusCode statusCode)
    {
        var handler = StubHttpMessageHandler.Returning(statusCode);

        var result = await CreateSender(handler, CreateChannel(provider)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo($"HTTP {(int)statusCode}"));
            Assert.That(handler.Requests.Single().RequestUri, Is.EqualTo(new Uri(CHANNEL_URL)));
            Assert.That(handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.Requests.Single().Content!.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            Assert.That(handler.Requests.Single().Content!.Headers.ContentType!.CharSet, Is.EqualTo("utf-8"));
        });
    }


    [Test]
    public async Task Send_Slack_PostsSlackPayload()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.TryGetProperty("blocks", out _), Is.True);
            Assert.That(document.RootElement.GetProperty("text").GetString(), Is.EqualTo("Hot lead\nReady to buy"));
        });
    }


    [Test]
    public async Task Send_Teams_PostsAdaptiveCardPayload()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.Accepted);

        await CreateSender(handler, CreateChannel(ChatProvider.Teams)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        using var document = JsonDocument.Parse(handler.RequestBodies.Single());
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("type").GetString(), Is.EqualTo("message"));
            Assert.That(document.RootElement.GetProperty("attachments")[0].GetProperty("contentType").GetString(), Is.EqualTo(TeamsPayloadBuilder.ADAPTIVE_CARD_CONTENT_TYPE));
        });
    }


    [Test]
    public async Task Send_SlackErrorStatus_AppendsShortErrorBody()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid_payload\n")
        }));

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("HTTP 400: invalid_payload"));
        });
    }


    [Test]
    public async Task Send_SlackLongErrorBody_Truncated()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(new string('e', 500))
        }));

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.That(result.Message, Has.Length.EqualTo("HTTP 404: ".Length + ChatMessageSender.MAX_ERROR_BODY_LENGTH));
    }


    [Test]
    public async Task Send_TeamsErrorStatus_StatusOnly()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":{\"code\":\"InvalidRequestContent\"}}")
        }));

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Teams)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("HTTP 400"));
        });
    }


    [Test]
    public async Task Send_UnknownChannel_ReturnsFailedWithoutRequest()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send("unknown", CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(handler.Requests, Is.Empty);
            Assert.That(result.Message, Is.EqualTo("Channel 'unknown' not configured in 'AutomationExtensions:ChatChannels:Channels' or has an invalid URL"));
        });
    }


    [TestCase("")]
    [TestCase("not a url")]
    [TestCase("/relative/path")]
    [TestCase("http://example.test/hooks/secret-token-123")]
    public async Task Send_InvalidUrl_ReturnsFailedWithoutRequest(string url)
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);
        var channel = CreateChannel(ChatProvider.Slack);
        channel.Url = url;

        var result = await CreateSender(handler, channel).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(handler.Requests, Is.Empty);
            Assert.That(result.Message, Does.Contain($"'{CHANNEL_NAME}'").And.Contain("invalid URL"));
            Assert.That(result.Message, Does.Not.Contain("secret-token-123"));
        });
    }


    [Test]
    public async Task Send_RequestThrows_ReturnsFailedWithoutUrl()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException($"Connection to {CHANNEL_URL} refused"));

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Teams)).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("Request failed"));
            Assert.That(result.Exception, Is.InstanceOf<HttpRequestException>());
        });
    }


    [Test]
    public async Task Send_ChannelTimesOut_ReturnsFailed()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var channel = CreateChannel(ChatProvider.Slack);
        channel.TimeoutSeconds = 0;

        var result = await CreateSender(handler, channel).Send(CHANNEL_NAME, CreateMessage(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("Timed out after 1 s"), "Timeout is at least 1 second.");
            Assert.That(result.Exception, Is.InstanceOf<OperationCanceledException>());
        });
    }


    [Test]
    public void Send_Cancelled_Propagates()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        Assert.That(
            async () => await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send(CHANNEL_NAME, CreateMessage(), cancellationSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }


    [Test]
    public async Task Send_ChannelNameCaseInsensitive()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        var result = await CreateSender(handler, CreateChannel(ChatProvider.Slack)).Send("SALES", CreateMessage(), CancellationToken.None);

        Assert.That(result.Succeeded, Is.True);
    }


    [TestCase(null, "")]
    [TestCase("  ", "")]
    [TestCase("no_text\r\n\tmore", "no_text more")]
    public void FormatErrorBody_SingleLine(string? body, string expected) => Assert.That(ChatMessageSender.FormatErrorBody(body), Is.EqualTo(expected));


    private static ChatMessageSender CreateSender(HttpMessageHandler handler, ChatChannelEndpointOptions channel)
    {
        var options = new AutomationExtensionsOptions();
        options.ChatChannels.Channels[CHANNEL_NAME] = channel;

        return new ChatMessageSender(
            new StubHttpClientFactory(handler),
            new TestOptionsMonitor<AutomationExtensionsOptions>(options),
            NullLoggerFactory.Instance);
    }


    private static ChatChannelEndpointOptions CreateChannel(ChatProvider provider) => new() { Provider = provider, Url = CHANNEL_URL };


    private static ChatMessage CreateMessage() => new("Hot lead", "Ready to buy", [("Process", "Lead nurture")]);
}
