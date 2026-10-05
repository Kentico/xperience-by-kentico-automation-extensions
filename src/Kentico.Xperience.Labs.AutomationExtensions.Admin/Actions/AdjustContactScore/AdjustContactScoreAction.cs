using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<AdjustContactScoreAction>(
    AdjustContactScoreAction.IDENTIFIER,
    "labs.automationextensions.adjustcontactscore.displayname",
    Description = "labs.automationextensions.adjustcontactscore.description",
    IconName = Icons.Graph)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Adds to, subtracts from, or sets an integer custom contact field (lightweight lead scoring), optionally limited to a range.
/// </summary>
/// <remarks>
/// Empty fields count as 0. The contact is read, changed, and saved without locking – when several processes adjust the
/// same field of the same contact at the same time, the last write wins.
/// </remarks>
public sealed class AdjustContactScoreAction(
    IAutomationComponentStatus componentStatus,
    IInfoProvider<ContactInfo> contactInfoProvider,
    ContactScoreFieldResolver fieldResolver,
    ILoggerFactory loggerFactory) : AutomationAction<AdjustContactScoreActionProperties>
{
    public const string NAME = "AdjustContactScore";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan invalidConfigurationLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly ContactScoreFieldResolver fieldResolver = fieldResolver;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.AdjustContactScore));


    /// <inheritdoc />
    public override async Task Execute(AdjustContactScoreActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.AdjustContactScore))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.AdjustContactScore, context.Process.DisplayName);
            return;
        }

        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return;
        }

        string? fieldName = fieldResolver.ResolveFieldName(properties.Field);
        string? error = ContactScoreCalculator.GetConfigurationError(properties, fieldName is not null, out var operation);
        if (error is not null)
        {
            LogInvalidConfiguration(context.Process.DisplayName, properties.Field, error);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contact = await context.GetProcessedObject(cancellationToken);
        int? storedValue = ContactScoreCalculator.GetStoredValue(contact.GetValue(fieldName!));
        int newValue = ContactScoreCalculator.Calculate(
            storedValue ?? 0,
            operation,
            properties.Amount,
            properties.LimitRange,
            properties.Minimum,
            properties.Maximum);

        if (storedValue == newValue)
        {
            return;
        }

        contact.SetValue(fieldName!, newValue);
        await contactInfoProvider.SetAsync(contact, cancellationToken);

        logger.LogInformation(
            LogEvents.ContactScoreAdjusted,
            "Automation '{ProcessName}', contact {ContactId}: field '{FieldName}' changed from {OldValue} to {NewValue}.",
            context.Process.DisplayName,
            contact.ContactID,
            fieldName,
            storedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(empty)",
            newValue);
    }


    private void LogInvalidConfiguration(string processName, string? fieldName, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{fieldName}", invalidConfigurationLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidContactScoreConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and does nothing: {Reason}.",
                IDENTIFIER,
                processName,
                reason));
}
