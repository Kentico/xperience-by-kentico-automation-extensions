using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterAutomationAction<PostChatMessageAction>(
    PostChatMessageAction.IDENTIFIER,
    "labs.automationextensions.postchatmessage.displayname",
    Description = "labs.automationextensions.postchatmessage.description",
    IconName = Icons.BubbleWithLines)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Posts a message about the processed contact to a named Microsoft Teams or Slack channel (e.g. a hot lead for the
/// sales team). Optionally records the outcome for the "Previous step succeeded" condition.
/// </summary>
/// <remarks>
/// Title and message use the tokens of <c>Set contact field</c>; unresolved tokens become empty text (warning logged).
/// Single attempt, no retries. Message contents, contact details, and webhook URLs are never logged.
/// </remarks>
public sealed class PostChatMessageAction(
    IChatMessageSender chatMessageSender,
    IAutomationProcessResolver processResolver,
    IAutomationComponentStatus componentStatus,
    ContactFieldResolver fieldResolver,
    ICustomAutomationTriggerProvider triggerProvider,
    IOptionsMonitor<AutomationExtensionsOptions> options,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationAction<PostChatMessageActionProperties>
{
    public const string NAME = "PostChatMessage";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan warningLogInterval = TimeSpan.FromHours(1);


    private readonly IChatMessageSender chatMessageSender = chatMessageSender;
    private readonly IAutomationProcessResolver processResolver = processResolver;
    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ContactFieldResolver fieldResolver = fieldResolver;
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;
    private readonly IOptionsMonitor<AutomationExtensionsOptions> options = options;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.PostChatMessage));


    /// <inheritdoc />
    public override async Task Execute(PostChatMessageActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        string processName = context.Process.DisplayName;

        if (!componentStatus.IsEnabled(AutomationComponentCatalog.PostChatMessage))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.PostChatMessage, processName);
            return;
        }

        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, processName);

            if (properties.RecordResult)
            {
                var skippedResult = StepResult.CreateSkipped(IDENTIFIER, timeProvider.GetUtcNow().UtcDateTime);
                await StepResultRecorder.Record(context, StepResultKeys.PostChatMessage.Name, properties.ResultNumber, skippedResult, cancellationToken);
            }

            return;
        }

        var contact = await context.GetProcessedObject(cancellationToken);
        var tokens = await ContactFieldTokenValues.Create(context, contact, fieldResolver, triggerProvider, timeProvider, cancellationToken);
        var contactDetails = properties.IncludeContactDetails
            ? new ChatContactDetails(contact.ContactEmail, contact.ContactFirstName, contact.ContactLastName, contact.ContactCompanyName)
            : null;
        string adminBaseUrl = options.CurrentValue.ChatChannels.AdminBaseUrl;
        var contactLink = AdminLink.ContactOverview(adminBaseUrl, contact.ContactID);
        var formSubmissionLink = ChatMessageFactory.GetFormSubmissionLink(adminBaseUrl, tokens.TriggerIdentifier, tokens.TriggerData);
        var processLink = await ChatMessageFactory.GetProcessLink(properties, adminBaseUrl, processName, processResolver, cancellationToken);

        var unresolvedTokens = new List<string>();
        var message = ChatMessageFactory.Create(properties, tokens, contactDetails, contactLink, formSubmissionLink, processLink, unresolvedTokens);

        foreach (string token in unresolvedTokens.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            LogUnresolvedToken(processName, token);
        }

        var result = await chatMessageSender.Send(properties.ChannelName, message, cancellationToken);

        if (result.Succeeded)
        {
            logger.LogInformation(
                LogEvents.ChatMessagePosted,
                "Chat message to channel '{ChannelName}' for contact {ContactId} in process '{ProcessName}' posted.",
                properties.ChannelName,
                contact.ContactID,
                processName);
        }
        else
        {
            logger.LogWarning(
                LogEvents.ChatMessageFailed,
                result.Exception,
                "Chat message to channel '{ChannelName}' for contact {ContactId} in process '{ProcessName}' failed: {Message}.",
                properties.ChannelName,
                contact.ContactID,
                processName,
                result.Message);
        }

        if (properties.RecordResult)
        {
            await StepResultRecorder.Record(context, StepResultKeys.PostChatMessage.Name, properties.ResultNumber, new StepResult
            {
                Succeeded = result.Succeeded,
                ActionIdentifier = IDENTIFIER,
                RecordedAt = timeProvider.GetUtcNow().UtcDateTime,
                Message = result.Message
            }, cancellationToken);
        }
    }


    private void LogUnresolvedToken(string processName, string token) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|TokenUnresolved|{processName}|{token.ToLowerInvariant()}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.ChatMessageTokensUnresolved,
                "Step '{ComponentIdentifier}' in process '{ProcessName}': token '{Token}' can't be resolved and is replaced by empty text.",
                IDENTIFIER,
                processName,
                token));
}
