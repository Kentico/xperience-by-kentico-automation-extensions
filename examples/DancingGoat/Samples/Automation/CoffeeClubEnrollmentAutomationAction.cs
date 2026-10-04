using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExperiments.Core;

using Microsoft.Extensions.Logging;

using Samples.DancingGoat;

[assembly: RegisterAutomationAction<CoffeeClubEnrollmentAutomationAction>(
    CoffeeClubEnrollmentAutomationAction.IDENTIFIER,
    CoffeeClubEnrollmentAutomationAction.DISPLAY_NAME,
    IconName = Icons.Cup,
    Description = "dancinggoat.automation.coffeeclubenrollment.description")]

namespace Samples.DancingGoat;

/// <summary>
/// Demo custom automation action that "enrolls" the contact in the Dancing Goat coffee club and opts in to the
/// "Previous step succeeded" condition of the automation experiments library.
/// </summary>
/// <remarks>
/// Opt-in consists of registering a step result source (<see cref="SOURCE_NAME"/>) via <c>AddStepResultSource</c> in
/// <c>Program.cs</c> and saving the outcome with <see cref="StepResultRecorder.Record"/>.
/// </remarks>
internal sealed class CoffeeClubEnrollmentAutomationAction : AutomationAction<CoffeeClubEnrollmentAutomationActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.CoffeeClubEnrollment";
    public const string DISPLAY_NAME = "dancinggoat.automation.coffeeclubenrollment.displayname";

    /// <summary>
    /// Name of the step result source registered for this action. Part of saved result keys – never change.
    /// </summary>
    public const string SOURCE_NAME = "DancingGoat.CoffeeClubEnrollment";


    private readonly TimeProvider timeProvider;
    private readonly ILogger<CoffeeClubEnrollmentAutomationAction> logger;


    /// <summary>
    /// Initializes a new instance of the <see cref="CoffeeClubEnrollmentAutomationAction"/> class.
    /// </summary>
    public CoffeeClubEnrollmentAutomationAction(TimeProvider timeProvider, ILogger<CoffeeClubEnrollmentAutomationAction> logger)
    {
        this.timeProvider = timeProvider;
        this.logger = logger;
    }


    /// <inheritdoc />
    public override async Task Execute(CoffeeClubEnrollmentAutomationActionProperties properties, AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (properties.SkipStep)
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);

            if (properties.RecordResult)
            {
                var skippedResult = StepResult.CreateSkipped(IDENTIFIER, timeProvider.GetUtcNow().UtcDateTime);
                await StepResultRecorder.Record(context, SOURCE_NAME, properties.ResultNumber, skippedResult, cancellationToken);
            }

            return;
        }

        var contact = await context.GetProcessedObject(cancellationToken);
        bool succeeded = IsSuccessful(properties.Outcome, contact);
        string message = succeeded ? "Enrolled" : "Not enrolled";

        logger.LogInformation(
            "Coffee club enrollment for contact {ContactId} in process '{ProcessName}': {Message} (outcome setting '{Outcome}').",
            contact.ContactID,
            context.Process.DisplayName,
            message,
            properties.Outcome);

        if (!properties.RecordResult)
        {
            return;
        }

        var result = new StepResult
        {
            Succeeded = succeeded,
            ActionIdentifier = IDENTIFIER,
            RecordedAt = timeProvider.GetUtcNow().UtcDateTime,
            Message = message
        };

        await StepResultRecorder.Record(context, SOURCE_NAME, properties.ResultNumber, result, cancellationToken);
    }


    private static bool IsSuccessful(string outcome, ContactInfo contact) => outcome switch
    {
        CoffeeClubEnrollmentOutcomes.ALWAYS_SUCCEED => true,
        CoffeeClubEnrollmentOutcomes.ALWAYS_FAIL => false,
        _ => !string.IsNullOrEmpty(contact.ContactEmail)
    };
}
