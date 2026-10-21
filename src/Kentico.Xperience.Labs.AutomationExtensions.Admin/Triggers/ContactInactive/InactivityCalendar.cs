namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Date calculations for inactivity matching.
/// </summary>
/// <remarks>
/// Xperience stores <c>ActivityCreated</c> in server local time, so pass the server-local date as <c>today</c>.
/// </remarks>
public static class InactivityCalendar
{
    /// <summary>
    /// Returns the day whose last activity makes a contact inactive for exactly <paramref name="inactiveDays"/> days on
    /// <paramref name="today"/>, and its local time range <c>[day 00:00, day + 1 00:00)</c>.
    /// </summary>
    public static InactivityRange GetRange(DateOnly today, int inactiveDays)
    {
        var lastActivityDate = today.AddDays(-inactiveDays);

        return new InactivityRange(
            lastActivityDate,
            lastActivityDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            lastActivityDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified));
    }
}
