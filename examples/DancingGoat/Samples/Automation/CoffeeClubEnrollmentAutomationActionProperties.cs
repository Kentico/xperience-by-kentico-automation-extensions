using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Samples.DancingGoat;

/// <summary>
/// Properties of <see cref="CoffeeClubEnrollmentAutomationAction"/>.
/// </summary>
internal sealed class CoffeeClubEnrollmentAutomationActionProperties : SkippableActionProperties
{
    /// <summary>
    /// Demo outcome of the enrollment (see <see cref="CoffeeClubEnrollmentOutcomes"/>).
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.automation.coffeeclubenrollment.outcome.label$}",
        ExplanationText = "{$dancinggoat.automation.coffeeclubenrollment.outcome.explanation$}",
        DataProviderType = typeof(CoffeeClubEnrollmentOutcomeOptionsProvider),
        Order = 10)]
    [RequiredValidationRule]
    public string Outcome { get; set; } = CoffeeClubEnrollmentOutcomes.AUTOMATIC;


    /// <summary>
    /// Saves the outcome for the "Previous step succeeded" condition.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$dancinggoat.automation.coffeeclubenrollment.recordresult.label$}",
        ExplanationText = "{$dancinggoat.automation.coffeeclubenrollment.recordresult.explanation$}",
        Order = 20)]
    public bool RecordResult { get; set; }


    /// <summary>
    /// Result number the outcome is saved under.
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.automation.coffeeclubenrollment.resultnumber.label$}",
        ExplanationText = "{$dancinggoat.automation.coffeeclubenrollment.resultnumber.explanation$}",
        DataProviderType = typeof(StepResultNumberOptionsProvider),
        Order = 30)]
    [VisibleIfTrue(nameof(RecordResult))]
    public string ResultNumber { get; set; } = StepResultKeys.DEFAULT_RESULT_NUMBER;
}
