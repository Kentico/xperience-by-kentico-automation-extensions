using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.WithinTimeWindow;

/// <summary>
/// Marketer-configurable properties of <see cref="WithinTimeWindowCondition"/>.
/// </summary>
public sealed class WithinTimeWindowProperties : SkippableConditionProperties
{
    /// <summary>
    /// Selected weekdays as individual checkbox properties.
    /// </summary>
    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.sunday$}",
        Order = 10)]
    public bool Sunday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.monday$}",
        Order = 11)]
    public bool Monday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.tuesday$}",
        Order = 12)]
    public bool Tuesday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.wednesday$}",
        Order = 13)]
    public bool Wednesday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.thursday$}",
        Order = 14)]
    public bool Thursday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.friday$}",
        Order = 15)]
    public bool Friday { get; set; }


    [CheckBoxComponent(
        Label = "{$labs.automationextensions.withintimewindow.weekday.saturday$}",
        Order = 16)]
    public bool Saturday { get; set; }


    /// <summary>
    /// Window start time in HH:mm format (e.g., "09:00").
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.withintimewindow.starttime.label$}",
        ExplanationText = "{$labs.automationextensions.withintimewindow.starttime.explanation$}",
        DataProviderType = typeof(TimeOfDayOptionsProvider),
        Order = 20)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.withintimewindow.starttime.required$}")]
    public string StartTime { get; set; } = "00:00";


    /// <summary>
    /// Window end time in HH:mm format (e.g., "17:00").
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.withintimewindow.endtime.label$}",
        ExplanationText = "{$labs.automationextensions.withintimewindow.endtime.explanation$}",
        DataProviderType = typeof(TimeOfDayOptionsProvider),
        Order = 30)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.withintimewindow.endtime.required$}")]
    public string EndTime { get; set; } = "00:00";


    /// <summary>
    /// Time zone ID from TimeZoneInfo.GetSystemTimeZones().
    /// </summary>
    [DropDownComponent(
        Label = "{$labs.automationextensions.withintimewindow.timezone.label$}",
        ExplanationText = "{$labs.automationextensions.withintimewindow.timezone.explanation$}",
        DataProviderType = typeof(TimeZoneOptionsProvider),
        Order = 40)]
    [RequiredValidationRule(ErrorMessage = "{$labs.automationextensions.withintimewindow.timezone.required$}")]
    public string TimeZoneId { get; set; } = "UTC";


    /// <summary>
    /// Optional inclusive start date.
    /// </summary>
    [DateInputComponent(
        Label = "{$labs.automationextensions.withintimewindow.startdate.label$}",
        ExplanationText = "{$labs.automationextensions.withintimewindow.startdate.explanation$}",
        Order = 50)]
    public DateTime? StartDate { get; set; }


    /// <summary>
    /// Optional inclusive end date.
    /// </summary>
    [DateInputComponent(
        Label = "{$labs.automationextensions.withintimewindow.enddate.label$}",
        ExplanationText = "{$labs.automationextensions.withintimewindow.enddate.explanation$}",
        Order = 60)]
    public DateTime? EndDate { get; set; }


    /// <summary>
    /// Gets selected weekdays as a list of DayOfWeek names.
    /// </summary>
    public IList<string> GetSelectedWeekdays()
    {
        var selected = new List<string>();
        if (Sunday)
        {
            selected.Add(DayOfWeek.Sunday.ToString());
        }

        if (Monday)
        {
            selected.Add(DayOfWeek.Monday.ToString());
        }

        if (Tuesday)
        {
            selected.Add(DayOfWeek.Tuesday.ToString());
        }

        if (Wednesday)
        {
            selected.Add(DayOfWeek.Wednesday.ToString());
        }

        if (Thursday)
        {
            selected.Add(DayOfWeek.Thursday.ToString());
        }

        if (Friday)
        {
            selected.Add(DayOfWeek.Friday.ToString());
        }

        if (Saturday)
        {
            selected.Add(DayOfWeek.Saturday.ToString());
        }

        return selected;
    }
}
