namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Outcome of a single library action execution.
/// </summary>
public sealed class StepResult
{
    /// <summary>
    /// Message saved for skipped steps.
    /// </summary>
    public const string SKIPPED_MESSAGE = "Skipped";


    /// <summary>
    /// Indicates whether the action completed successfully. Always <c>false</c> for skipped steps.
    /// </summary>
    public bool Succeeded { get; set; }


    /// <summary>
    /// Indicates whether the action was skipped by the marketer (see <see cref="SkippableActionProperties"/>).
    /// </summary>
    public bool Skipped { get; set; }


    /// <summary>
    /// Identifier of the action that produced the result.
    /// </summary>
    public string ActionIdentifier { get; set; } = string.Empty;


    /// <summary>
    /// Time the result was recorded (UTC).
    /// </summary>
    public DateTime RecordedAt { get; set; }


    /// <summary>
    /// Short, non-personal description of the outcome (e.g. HTTP status code).
    /// </summary>
    public string Message { get; set; } = string.Empty;


    /// <summary>
    /// Creates the result saved by a skipped action. Conditions treat it as not succeeded.
    /// </summary>
    public static StepResult CreateSkipped(string actionIdentifier, DateTime recordedAt) => new()
    {
        Succeeded = false,
        Skipped = true,
        ActionIdentifier = actionIdentifier,
        RecordedAt = recordedAt,
        Message = SKIPPED_MESSAGE
    };
}
