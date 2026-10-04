using System.Globalization;

using Kentico.Xperience.Labs.AutomationExperiments.Core;

namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;

/// <summary>
/// Formats saved step results for log output.
/// </summary>
public static class StepResultSummary
{
    public const string NO_RESULTS = "none";


    /// <summary>
    /// Returns results ordered by key, e.g. <c>SendWebhook.1: succeeded (HTTP 202, 2026-10-04T12:00:00Z)</c>, separated by <c>; </c>.
    /// </summary>
    public static string Format(StepResultData? data)
    {
        if (data is null || data.Results.Count == 0)
        {
            return NO_RESULTS;
        }

        var entries = data.Results
            .OrderBy(result => result.Key, StringComparer.OrdinalIgnoreCase)
            .Select(result => string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1} ({2}, {3:yyyy-MM-ddTHH:mm:ssZ})",
                result.Key,
                GetOutcome(result.Value),
                result.Value.Message,
                result.Value.RecordedAt));

        return string.Join("; ", entries);
    }


    private static string GetOutcome(StepResult result)
    {
        if (result.Skipped)
        {
            return "skipped";
        }

        return result.Succeeded ? "succeeded" : "failed";
    }
}
