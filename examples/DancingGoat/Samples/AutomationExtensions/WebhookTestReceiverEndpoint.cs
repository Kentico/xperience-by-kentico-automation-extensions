using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Samples.DancingGoat;

/// <summary>
/// Development-only endpoint receiving payloads from the "Send webhook" automation action.
/// Logs the payload, verifies the signature, and returns the status code requested in the <c>status</c> query parameter
/// (e.g. <c>?status=500</c>) so both branches of "Previous step succeeded" can be tested.
/// </summary>
internal static class WebhookTestReceiverEndpoint
{
    public const string ROUTE = "/automation-extensions/webhook-test";
    public const string ENDPOINT_NAME = "local-test";


    public static IEndpointRouteBuilder MapWebhookTestReceiver(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(ROUTE, Receive);

        return endpoints;
    }


    private static async Task<IResult> Receive(
        HttpRequest request,
        IOptionsMonitor<AutomationExtensionsOptions> options,
        ILoggerFactory loggerFactory,
        int? status)
    {
        var logger = loggerFactory.CreateLogger(nameof(WebhookTestReceiverEndpoint));

        using var reader = new System.IO.StreamReader(request.Body, Encoding.UTF8);
        string body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);

        string signatureState = GetSignatureState(request, options.CurrentValue, body);
        int statusCode = status ?? StatusCodes.Status200OK;

        logger.LogInformation(
            "Webhook test receiver got payload (signature {SignatureState}), responding {StatusCode}: {Body}",
            signatureState,
            statusCode,
            body);

        return Results.StatusCode(statusCode);
    }


    private static string GetSignatureState(HttpRequest request, AutomationExtensionsOptions options, string body)
    {
        if (!options.Webhooks.Endpoints.TryGetValue(ENDPOINT_NAME, out var endpoint) || string.IsNullOrEmpty(endpoint.Secret))
        {
            return "not configured";
        }

        string received = request.Headers[endpoint.SignatureHeaderName].ToString();
        string expected = WebhookSignature.Compute(endpoint.Secret, body);
        bool isValid = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), Encoding.UTF8.GetBytes(expected));

        return isValid ? "valid" : "INVALID";
    }
}
