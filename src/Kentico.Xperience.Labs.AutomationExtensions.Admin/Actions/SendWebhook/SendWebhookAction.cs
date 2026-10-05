using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<SendWebhookAction>(
    SendWebhookAction.IDENTIFIER,
    "labs.automationextensions.sendwebhook.displayname",
    Description = "labs.automationextensions.sendwebhook.description",
    IconName = Icons.ArrowRightTopSquare)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;

/// <summary>
/// Posts a JSON payload describing the processed contact to a named webhook endpoint (CRM, CDP, Zapier, Power Automate, …).
/// Optionally records the outcome for the "Previous step succeeded" condition.
/// </summary>
public sealed class SendWebhookAction(
    IWebhookSender webhookSender,
    IAutomationComponentStatus componentStatus,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationAction<SendWebhookActionProperties>
{
    public const string NAME = "SendWebhook";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IWebhookSender webhookSender = webhookSender;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.SendWebhook));


    /// <inheritdoc />
    public override async Task Execute(SendWebhookActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.SendWebhook))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.SendWebhook, context.Process.DisplayName);
            return;
        }

        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);

            if (properties.RecordResult)
            {
                var skippedResult = StepResult.CreateSkipped(IDENTIFIER, timeProvider.GetUtcNow().UtcDateTime);
                await StepResultRecorder.Record(context, StepResultKeys.SendWebhook.Name, properties.ResultNumber, skippedResult, cancellationToken);
            }

            return;
        }

        var contact = await context.GetProcessedObject(cancellationToken);
        var payload = new WebhookPayload
        {
            EventName = properties.EventName,
            OccurredAt = timeProvider.GetUtcNow(),
            ProcessName = context.Process.DisplayName,
            TriggerIdentifier = await context.GetTriggerIdentifier(cancellationToken) ?? string.Empty,
            Contact = CreateContact(contact, properties.IncludeContactDetails)
        };

        var result = await webhookSender.Send(properties.EndpointName, payload, cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning(
                LogEvents.WebhookFailed,
                result.Exception,
                "Webhook '{EventName}' to endpoint '{EndpointName}' for contact {ContactId} in process '{ProcessName}' failed: {Message}.",
                properties.EventName,
                properties.EndpointName,
                contact.ContactID,
                context.Process.DisplayName,
                result.Message);
        }

        if (properties.RecordResult)
        {
            await StepResultRecorder.Record(context, StepResultKeys.SendWebhook.Name, properties.ResultNumber, new StepResult
            {
                Succeeded = result.Succeeded,
                ActionIdentifier = IDENTIFIER,
                RecordedAt = timeProvider.GetUtcNow().UtcDateTime,
                Message = result.Message
            }, cancellationToken);
        }
    }


    private static WebhookContact CreateContact(ContactInfo contact, bool includeDetails)
    {
        if (!includeDetails)
        {
            return new WebhookContact { Guid = contact.ContactGUID };
        }

        return new WebhookContact
        {
            Guid = contact.ContactGUID,
            Email = contact.ContactEmail,
            FirstName = contact.ContactFirstName,
            LastName = contact.ContactLastName,
            CompanyName = contact.ContactCompanyName
        };
    }
}
