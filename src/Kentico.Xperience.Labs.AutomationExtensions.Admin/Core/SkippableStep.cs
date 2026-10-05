using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Shared constants and helpers for skippable steps (<see cref="SkippableActionProperties"/>, <see cref="SkippableConditionProperties"/>).
/// </summary>
public static class SkippableStep
{
    /// <summary>
    /// Order of the "Step behavior" category. High enough to keep it below the step's own properties.
    /// </summary>
    public const int CATEGORY_ORDER = 10000;

    public const string BRANCH_YES = "yes";
    public const string BRANCH_NO = "no";


    /// <summary>
    /// Returns <c>true</c> when the condition is skipped; <paramref name="result"/> then holds the branch to follow
    /// (<c>true</c> = Yes). Unknown branch values follow No, the safe default.
    /// </summary>
    public static bool TryGetSkippedResult(SkippableConditionProperties properties, out bool result)
    {
        result = properties.SkipStep && string.Equals(properties.SkippedBranch, BRANCH_YES, StringComparison.OrdinalIgnoreCase);

        return properties.SkipStep;
    }


    /// <summary>
    /// Logs (at debug level) that a contact passed a skipped step.
    /// </summary>
    public static void LogSkipped(ILogger logger, string componentIdentifier, string processName) =>
        logger.LogDebug(LogEvents.StepSkipped, "Step '{ComponentIdentifier}' in process '{ProcessName}' is skipped.", componentIdentifier, processName);
}
