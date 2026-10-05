using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<ContactNumberFieldCompareCondition>(
    ContactNumberFieldCompareCondition.IDENTIFIER,
    "labs.automationextensions.contactnumberfieldcompare.displayname",
    Description = "labs.automationextensions.contactnumberfieldcompare.description",
    IconName = Icons.Gauge)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

/// <summary>
/// Compares an integer custom contact field (e.g. a score set by <see cref="AdjustContactScoreAction"/>) with a value.
/// Empty fields count as 0. Read-only – never changes the contact.
/// </summary>
public sealed class ContactNumberFieldCompareCondition(
    IAutomationComponentStatus componentStatus,
    ContactScoreFieldResolver fieldResolver,
    ILoggerFactory loggerFactory) : AutomationCondition<ContactNumberFieldCompareProperties>
{
    public const string NAME = "ContactNumberFieldCompare";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan invalidConfigurationLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ContactScoreFieldResolver fieldResolver = fieldResolver;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ContactNumberFieldCompare));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(
        ContactNumberFieldCompareProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.ContactNumberFieldCompare))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.ContactNumberFieldCompare, context.Process.DisplayName, "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        string? fieldName = fieldResolver.ResolveFieldName(properties.Field);
        string? error = NumberComparison.GetConfigurationError(properties, fieldName is not null, out var comparisonOperator);
        if (error is not null)
        {
            LogInvalidConfiguration(context.Process.DisplayName, properties.Field, error);
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contact = await context.GetProcessedObject(cancellationToken);
        int current = ContactScoreCalculator.GetStoredValue(contact.GetValue(fieldName!)) ?? 0;
        bool result = NumberComparison.Compare(current, comparisonOperator, properties.Value);

        logger.LogDebug(
            LogEvents.ContactNumberFieldCompared,
            "Automation '{ProcessName}', contact {ContactId}: field '{FieldName}' value {CurrentValue} {Operator} {Value} is {Result}.",
            context.Process.DisplayName,
            contact.ContactID,
            fieldName,
            current,
            comparisonOperator,
            properties.Value,
            result);

        return result;
    }


    private void LogInvalidConfiguration(string processName, string? fieldName, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{fieldName}", invalidConfigurationLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidNumberCompareConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and sends all contacts through the No branch: {Reason}.",
                IDENTIFIER,
                processName,
                reason));
}
