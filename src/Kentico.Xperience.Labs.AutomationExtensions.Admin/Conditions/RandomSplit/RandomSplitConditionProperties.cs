using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;

/// <summary>
/// Marketer-configurable properties of <see cref="RandomSplitCondition"/>.
/// </summary>
public sealed class RandomSplitConditionProperties : SkippableConditionProperties
{
    /// <summary>
    /// Percentage of contacts assigned to the Yes branch.
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.randomsplit.percentage.label$}",
        ExplanationText = "{$labs.automationextensions.randomsplit.percentage.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 10)]
    [MinimumIntegerValueValidationRule(0, ErrorMessage = "{$labs.automationextensions.randomsplit.percentage.minimum$}")]
    [MaximumIntegerValueValidationRule(100, ErrorMessage = "{$labs.automationextensions.randomsplit.percentage.maximum$}")]
    public int Percentage { get; set; } = 50;


    /// <summary>
    /// Stable identity key that separates this split from other splits.
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.randomsplit.splitkey.label$}",
        ExplanationText = "{$labs.automationextensions.randomsplit.splitkey.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 20)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.randomsplit.splitkey.required$}")]
    public string SplitKey { get; set; } = "default";
}
