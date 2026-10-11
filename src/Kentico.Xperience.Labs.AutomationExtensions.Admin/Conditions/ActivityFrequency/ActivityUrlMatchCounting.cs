using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Sums the activity counts of the distinct URLs that match URL rules.
/// </summary>
internal static class ActivityUrlMatchCounting
{
    /// <summary>
    /// Matches each distinct value once and sums the counts of the matching ones. Empty (NULL) values match only negated
    /// rules, like unrecognized URLs.
    /// </summary>
    /// <returns>Number of matching activities, and whether a wildcard comparison timed out (counted as "no match").</returns>
    public static (int Count, bool TimedOut) Sum(IEnumerable<ActivityValueCount> groups, UrlMatchRuleSet ruleSet)
    {
        var matcher = UrlMatcher.Create(ruleSet);
        int count = 0;
        bool timedOut = false;

        foreach (var group in groups)
        {
            var evaluation = matcher.Evaluate(group.Value);
            timedOut |= evaluation.TimedOut;
            if (evaluation.IsMatch)
            {
                count += group.Count;
            }
        }

        return (count, timedOut);
    }
}
