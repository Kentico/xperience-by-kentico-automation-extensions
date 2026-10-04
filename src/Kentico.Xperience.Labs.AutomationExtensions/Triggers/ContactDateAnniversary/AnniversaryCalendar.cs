namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Date calculations for anniversary matching.
/// </summary>
public static class AnniversaryCalendar
{
    /// <summary>
    /// Returns the month/day pairs whose anniversary falls on <paramref name="targetDate"/>.
    /// February 29 dates are matched on February 28 in non-leap years.
    /// </summary>
    public static IReadOnlyList<(int Month, int Day)> GetMatchingMonthDays(DateOnly targetDate)
    {
        bool isFebruary28InCommonYear = targetDate is { Month: 2, Day: 28 } && !DateTime.IsLeapYear(targetDate.Year);

        if (isFebruary28InCommonYear)
        {
            return [(2, 28), (2, 29)];
        }

        return [(targetDate.Month, targetDate.Day)];
    }


    /// <summary>
    /// Returns the number of whole years between <paramref name="originalDate"/> and its anniversary on <paramref name="targetDate"/>,
    /// or <c>null</c> when the original date lies after the target year.
    /// </summary>
    public static int? GetYears(DateOnly originalDate, DateOnly targetDate)
    {
        int years = targetDate.Year - originalDate.Year;

        return years >= 0 ? years : null;
    }
}
