using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Property validation of <see cref="ActivityFrequencyCondition"/>.
/// </summary>
internal static class ActivityFrequencyConfiguration
{
    public const int MIN_DAYS = 1;
    public const int MAX_DAYS = 365;


    /// <summary>
    /// Returns why the properties can't be evaluated, or <c>null</c> when they are valid.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="activityTypeExists">Whether <see cref="ActivityFrequencyProperties.ActivityType"/> is an existing activity type (enabled or not).</param>
    /// <param name="comparisonOperator">Parsed operator when valid.</param>
    public static string? GetConfigurationError(ActivityFrequencyProperties properties, bool activityTypeExists, out NumberComparisonOperator comparisonOperator)
    {
        comparisonOperator = default;

        if (string.IsNullOrWhiteSpace(properties.ActivityType))
        {
            return "no activity type is selected";
        }

        if (!activityTypeExists)
        {
            return $"activity type '{properties.ActivityType}' doesn't exist";
        }

        if (!NumberComparison.TryParseOperator(properties.Operator, out comparisonOperator))
        {
            return $"operator '{properties.Operator}' is unknown";
        }

        if (properties.Count < 0)
        {
            return $"number of activities {properties.Count} is negative";
        }

        if (properties.Days is < MIN_DAYS or > MAX_DAYS)
        {
            return $"number of days {properties.Days} is outside {MIN_DAYS}–{MAX_DAYS}";
        }

        return null;
    }
}
