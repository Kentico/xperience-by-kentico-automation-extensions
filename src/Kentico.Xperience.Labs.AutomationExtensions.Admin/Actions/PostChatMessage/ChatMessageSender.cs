using System.Text;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <inheritdoc />
/// <remarks>
/// Single attempt, no retries. Any 2xx status is success (Slack returns 200 <c>ok</c>; Teams Workflows usually 202).
/// The webhook URL is a secret and is never logged or returned.
/// </remarks>
internal sealed class ChatMessageSender(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILoggerFactory loggerFactory) : IChatMessageSender
{
    /// <summary>
    /// Name of the <see cref="HttpClient"/> registered for chat webhook requests.
    /// </summary>
    public const string HTTP_CLIENT_NAME = "Labs.AutomationExtensions.ChatChannels";

    /// <summary>
    /// Maximum length of the Slack error text (e.g. <c>invalid_payload</c>) appended to a failure message.
    /// </summary>
    public const int MAX_ERROR_BODY_LENGTH = 200;


    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.PostChatMessage));


    /// <inheritdoc />
    public async Task<ChatSendResult> Send(string channelName, ChatMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(channelName)
            || !options.CurrentValue.ChatChannels.Channels.TryGetValue(channelName, out var channel)
            || !Uri.TryCreate(channel.Url, UriKind.Absolute, out var channelUri)
            || channelUri.Scheme != Uri.UriSchemeHttps
            || !Enum.IsDefined(channel.Provider))
        {
            return new ChatSendResult(
                false,
                $"Channel '{channelName}' not configured in '{AutomationExtensionsOptions.SECTION_NAME}:ChatChannels:Channels' or has an invalid URL");
        }

        string body = channel.Provider == ChatProvider.Slack
            ? SlackPayloadBuilder.Build(message)
            : TeamsPayloadBuilder.Build(message);

        using var request = new HttpRequestMessage(HttpMethod.Post, channelUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        int timeoutSeconds = Math.Max(1, channel.TimeoutSeconds);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var client = httpClientFactory.CreateClient(HTTP_CLIENT_NAME);
            using var response = await client.SendAsync(request, timeoutSource.Token);

            int statusCode = (int)response.StatusCode;
            logger.LogDebug(LogEvents.ChatMessageResponse, "Chat channel '{ChannelName}' returned status code {StatusCode}.", channelName, statusCode);

            if (response.IsSuccessStatusCode)
            {
                return new ChatSendResult(true, $"HTTP {statusCode}");
            }

            string error = channel.Provider == ChatProvider.Slack
                ? FormatErrorBody(await response.Content.ReadAsStringAsync(timeoutSource.Token))
                : string.Empty;

            return new ChatSendResult(false, error.Length > 0 ? $"HTTP {statusCode}: {error}" : $"HTTP {statusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new ChatSendResult(false, "Request failed", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new ChatSendResult(false, $"Timed out after {timeoutSeconds} s", ex);
        }
    }


    /// <summary>
    /// Returns the response body as a single line of at most <see cref="MAX_ERROR_BODY_LENGTH"/> characters.
    /// </summary>
    internal static string FormatErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        string singleLine = string.Join(' ', body.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return ChatText.Truncate(singleLine, MAX_ERROR_BODY_LENGTH);
    }
}
