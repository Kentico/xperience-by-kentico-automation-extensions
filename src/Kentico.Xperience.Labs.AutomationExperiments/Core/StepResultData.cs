using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Process data holding outcomes of library actions, keyed by the marketer-selected result key
/// (see <see cref="StepResultKeys"/>). Read by conditions such as "Previous step succeeded".
/// </summary>
/// <remarks>
/// A single instance is shared by all steps of a process run, so results are stored per key to keep them isolated.
/// </remarks>
public sealed class StepResultData : IAutomationProcessData
{
    /// <inheritdoc />
    public static string Identifier => "Labs.AutomationExperiments.StepResultData";


    /// <summary>
    /// Step outcomes keyed by result key (e.g. <c>SendWebhook.1</c>).
    /// </summary>
    public Dictionary<string, StepResult> Results { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
