using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Marketer-configurable properties of <see cref="ActivityFrequencyCondition"/>.
/// </summary>
/// <remarks>
/// Detail filters (operator + text) are shown only for activity types they apply to (<see cref="ActivityDetailFilters"/>);
/// hidden filters are ignored at runtime. Empty text = no filter.
/// </remarks>
public sealed class ActivityFrequencyProperties : SkippableConditionProperties
{
    /// <summary>
    /// Code name of the activity type to count.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.activityfrequency.activitytype.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.activitytype.explanation$}",
        DataProviderType = typeof(ActivityTypeOptionsProvider),
        Order = 10)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.activityfrequency.activitytype.required$}")]
    public string ActivityType { get; set; } = string.Empty;


    /// <summary>
    /// Match operator name (<see cref="TextMatchOperator"/>) of the <see cref="Url"/> filter.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.activityfrequency.urloperator.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.textoperator.explanation$}",
        DataProviderType = typeof(TextMatchOperatorOptionsProvider),
        Order = 20)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Url)]
    public string UrlOperator { get; set; } = nameof(TextMatchOperator.Contains);


    /// <summary>
    /// Optional page URL text (page visit, landing page, form submission).
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.activityfrequency.url.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.url.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 30)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Url)]
    public string Url { get; set; } = string.Empty;


    /// <summary>
    /// Match operator name (<see cref="TextMatchOperator"/>) of the <see cref="Title"/> filter.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.activityfrequency.titleoperator.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.textoperator.explanation$}",
        DataProviderType = typeof(TextMatchOperatorOptionsProvider),
        Order = 40)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Title)]
    public string TitleOperator { get; set; } = nameof(TextMatchOperator.Contains);


    /// <summary>
    /// Optional activity title text (form submission, member registration).
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.activityfrequency.title.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.title.explanation$}",
        Order = 50)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Title)]
    public string Title { get; set; } = string.Empty;


    /// <summary>
    /// Match operator name (<see cref="TextMatchOperator"/>) of the <see cref="Value"/> filter.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.activityfrequency.valueoperator.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.textoperator.explanation$}",
        DataProviderType = typeof(TextMatchOperatorOptionsProvider),
        Order = 60)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Value)]
    public string ValueOperator { get; set; } = nameof(TextMatchOperator.Contains);


    /// <summary>
    /// Optional activity value text (email link click, custom and other types).
    /// </summary>
    [TextInputComponent(
        Label = "{$labs.automationextensions.activityfrequency.value.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.value.explanation$}",
        ExplanationTextAsHtml = true,
        Order = 70)]
    [VisibleIfActivityDetailFilter(nameof(ActivityType), ActivityDetailFilter.Value)]
    public string Value { get; set; } = string.Empty;


    /// <summary>
    /// Operator name (<see cref="NumberComparisonOperator"/>), shared with <see cref="ContactNumberFieldCompareCondition"/>.
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.activityfrequency.operator.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.operator.explanation$}",
        DataProviderType = typeof(NumberComparisonOperatorOptionsProvider),
        Order = 80)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.activityfrequency.operator.required$}")]
    public string Operator { get; set; } = nameof(NumberComparisonOperator.GreaterThanOrEqual);


    /// <summary>
    /// Number of activities the count is compared with.
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.activityfrequency.count.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.count.explanation$}",
        Order = 90)]
    [MinimumIntegerValueValidationRule(0, ErrorMessage = "{$labs.automationextensions.activityfrequency.count.minimum$}")]
    public int Count { get; set; } = 3;


    /// <summary>
    /// Length of the rolling window in days (<see cref="ActivityFrequencyConfiguration.MIN_DAYS"/>–<see cref="ActivityFrequencyConfiguration.MAX_DAYS"/>).
    /// </summary>
    [NumberInputComponent(
        Label = "{$labs.automationextensions.activityfrequency.days.label$}",
        ExplanationText = "{$labs.automationextensions.activityfrequency.days.explanation$}",
        Order = 100)]
    [MinimumIntegerValueValidationRule(ActivityFrequencyConfiguration.MIN_DAYS, ErrorMessage = "{$labs.automationextensions.activityfrequency.days.minimum$}")]
    [MaximumIntegerValueValidationRule(ActivityFrequencyConfiguration.MAX_DAYS, ErrorMessage = "{$labs.automationextensions.activityfrequency.days.maximum$}")]
    public int Days { get; set; } = 7;
}
