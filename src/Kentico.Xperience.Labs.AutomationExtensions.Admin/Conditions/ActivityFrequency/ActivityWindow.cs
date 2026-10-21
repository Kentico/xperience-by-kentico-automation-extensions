namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Rolling time window of <see cref="ActivityFrequencyCondition"/>.
/// </summary>
/// <remarks>
/// Xperience stores <c>ActivityCreated</c> in server local time (<c>DateTime.Now</c>), and the built-in "in the last N days"
/// rules compare it with local now minus N days. Pass local now (<see cref="TimeProvider.GetLocalNow"/>) to stay in the same basis.
/// </remarks>
internal static class ActivityWindow
{
    /// <summary>
    /// Returns the start of the window: exactly <paramref name="days"/> × 24 hours before <paramref name="now"/>.
    /// </summary>
    public static DateTime GetSince(DateTime now, int days) => now.AddDays(-days);
}
