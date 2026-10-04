using System.Text;
using System.Text.Json;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <inheritdoc />
internal sealed class WebhookSender(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    ILoggerFactory loggerFactory) : IWebhookSender
{
    /// <summary>
    /// Name of the <see cref="HttpClient"/> registered for webhook requests.
    /// </summary>
    public const string HTTP_CLIENT_NAME = "Labs.AutomationExtensions.Webhooks";


    private static readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);


    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.SendWebhook));


    /// <inheritdoc />
    public async Task<WebhookSendResult> Send(string endpointName, WebhookPayload payload, CancellationToken cancellationToken)
    {
        if (!options.CurrentValue.Webhooks.Endpoints.TryGetValue(endpointName, out var endpoint)
            || !Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var endpointUri))
        {
            return new WebhookSendResult(
                false,
                $"Endpoint '{endpointName}' not configured in '{AutomationExtensionsOptions.SECTION_NAME}:Webhooks:Endpoints' or has an invalid URL");
        }

        string body = JsonSerializer.Serialize(payload, serializerOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(endpoint.Secret))
        {
            request.Headers.Add(endpoint.SignatureHeaderName, WebhookSignature.Compute(endpoint.Secret, body));
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, endpoint.TimeoutSeconds)));

        try
        {
            var client = httpClientFactory.CreateClient(HTTP_CLIENT_NAME);
            using var response = await client.SendAsync(request, timeoutSource.Token);

            int statusCode = (int)response.StatusCode;
            logger.LogDebug(LogEvents.WebhookResponse, "Webhook endpoint '{EndpointName}' returned status code {StatusCode}.", endpointName, statusCode);

            return new WebhookSendResult(response.IsSuccessStatusCode, $"HTTP {statusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new WebhookSendResult(false, "Request failed", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new WebhookSendResult(false, $"Timed out after {endpoint.TimeoutSeconds} s", ex);
        }
    }
}
