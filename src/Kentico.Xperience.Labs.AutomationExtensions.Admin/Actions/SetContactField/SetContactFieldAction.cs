using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<SetContactFieldAction>(
    SetContactFieldAction.IDENTIFIER,
    "labs.automationextensions.setcontactfield.displayname",
    Description = "labs.automationextensions.setcontactfield.description",
    IconName = Icons.EditInSquare)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Sets a contact field (system or custom) to a value built from tokens (date, process, trigger data, other fields), or
/// calculates a number from the field's current value. Saves the outcome for the "Previous step succeeded" condition.
/// </summary>
/// <remarks>
/// Unresolved tokens become empty text (warning logged). The step fails – without saving – only when the value can't be
/// converted to the field type, the calculation fails (division by zero, overflow, invalid operand), the configuration is
/// invalid, or saving fails. The contact is read, changed, and saved without locking – the last write wins.
/// Values are never logged (they may be personal data).
/// </remarks>
public sealed class SetContactFieldAction(
    IAutomationComponentStatus componentStatus,
    IInfoProvider<ContactInfo> contactInfoProvider,
    ContactFieldResolver fieldResolver,
    ICustomAutomationTriggerProvider triggerProvider,
    TimeProvider timeProvider,
    ILoggerFactory loggerFactory) : AutomationAction<SetContactFieldActionProperties>
{
    public const string NAME = "SetContactField";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private static readonly TimeSpan warningLogInterval = TimeSpan.FromHours(1);


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly IInfoProvider<ContactInfo> contactInfoProvider = contactInfoProvider;
    private readonly ContactFieldResolver fieldResolver = fieldResolver;
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;
    private readonly TimeProvider timeProvider = timeProvider;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.SetContactField));


    /// <inheritdoc />
    public override async Task Execute(SetContactFieldActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        string processName = context.Process.DisplayName;

        if (!componentStatus.IsEnabled(AutomationComponentCatalog.SetContactField))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.SetContactField, processName);
            return;
        }

        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, processName);
            await RecordResult(context, properties, StepResult.CreateSkipped(IDENTIFIER, GetUtcNow()), cancellationToken);
            return;
        }

        var field = fieldResolver.Resolve(properties.Field);
        string? error = SetContactFieldEvaluator.GetConfigurationError(properties, field, out var mode, out var arithmeticOperator);
        if (error is not null)
        {
            LogInvalidConfiguration(processName, properties.Field, error);
            await RecordResult(context, properties, false, SetContactFieldEvaluator.MESSAGE_INVALID_CONFIGURATION, cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contact = await context.GetProcessedObject(cancellationToken);
        var tokens = await ContactFieldTokenValues.Create(context, contact, fieldResolver, triggerProvider, timeProvider, cancellationToken);
        object? currentValue = contact.GetValue(field!.Name);

        var outcome = SetContactFieldEvaluator.Evaluate(properties, field, mode, arithmeticOperator, currentValue, tokens);

        foreach (string token in outcome.UnresolvedTokens.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            LogUnresolvedToken(processName, token);
        }

        if (outcome.Truncated)
        {
            LogTruncated(processName, field);
        }

        if (outcome.Kind == SetContactFieldOutcomeKind.Failed)
        {
            LogFailed(processName, contact.ContactID, field.Name, outcome.Error!);
            await RecordResult(context, properties, false, outcome.Message, cancellationToken);
            return;
        }

        if (outcome.Kind == SetContactFieldOutcomeKind.Write && !await TrySave(contact, field.Name, currentValue, outcome.NewValue, processName, cancellationToken))
        {
            await RecordResult(context, properties, false, SetContactFieldEvaluator.MESSAGE_SAVE_FAILED, cancellationToken);
            return;
        }

        logger.LogDebug(
            LogEvents.ContactFieldSet,
            "Automation '{ProcessName}', contact {ContactId}: field '{FieldName}' {Outcome}.",
            processName,
            contact.ContactID,
            field.Name,
            outcome.Message);

        await RecordResult(context, properties, true, outcome.Message, cancellationToken);
    }


    private async Task<bool> TrySave(ContactInfo contact, string fieldName, object? currentValue, object? newValue, string processName, CancellationToken cancellationToken)
    {
        contact.SetValue(fieldName, newValue);

        try
        {
            await contactInfoProvider.SetAsync(contact, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            // The engine reuses the same contact instance in later steps – keep it in sync with the database.
            contact.SetValue(fieldName, currentValue is DBNull ? null : currentValue);

            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            // The exception message isn't logged: database errors can contain the value (personal data).
            LogFailed(processName, contact.ContactID, fieldName, $"saving the contact failed ({exception.GetType().Name})");
            return false;
        }
    }


    private DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;


    private Task RecordResult(AutomationProcessContext context, SetContactFieldActionProperties properties, bool succeeded, string message, CancellationToken cancellationToken) =>
        RecordResult(context, properties, new StepResult
        {
            Succeeded = succeeded,
            ActionIdentifier = IDENTIFIER,
            RecordedAt = GetUtcNow(),
            Message = message
        }, cancellationToken);


    private static Task RecordResult(AutomationProcessContext context, SetContactFieldActionProperties properties, StepResult result, CancellationToken cancellationToken) =>
        StepResultRecorder.Record(context, StepResultKeys.SetContactField.Name, properties.ResultNumber, result, cancellationToken);


    private void LogInvalidConfiguration(string processName, string? fieldName, string reason) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|InvalidConfiguration|{processName}|{fieldName}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.InvalidSetContactFieldConfiguration,
                "Step '{ComponentIdentifier}' in process '{ProcessName}' is not configured correctly and changes nothing: {Reason}.",
                IDENTIFIER,
                processName,
                reason));


    private void LogUnresolvedToken(string processName, string token) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|TokenUnresolved|{processName}|{token.ToLowerInvariant()}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.SetContactFieldTokenUnresolved,
                "Step '{ComponentIdentifier}' in process '{ProcessName}': token '{Token}' can't be resolved and is replaced by empty text.",
                IDENTIFIER,
                processName,
                token));


    private void LogTruncated(string processName, ContactField field) =>
        logger.LogWithIntervalPolicy(
            LoggingIntervalPolicy.OncePerPeriod($"{IDENTIFIER}|Truncated|{processName}|{field.Name}", warningLogInterval),
            l => l.LogWarning(
                LogEvents.SetContactFieldValueTruncated,
                "Step '{ComponentIdentifier}' in process '{ProcessName}': value for field '{FieldName}' is longer than {FieldSize} characters and is truncated.",
                IDENTIFIER,
                processName,
                field.Name,
                field.Size));


    private void LogFailed(string processName, int contactId, string fieldName, string reason) =>
        logger.LogWarning(
            LogEvents.SetContactFieldFailed,
            "Automation '{ProcessName}', contact {ContactId}: field '{FieldName}' not changed: {Reason}.",
            processName,
            contactId,
            fieldName,
            reason);
}
