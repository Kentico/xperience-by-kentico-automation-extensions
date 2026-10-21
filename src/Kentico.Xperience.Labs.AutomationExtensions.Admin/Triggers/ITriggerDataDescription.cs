namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers;

/// <summary>
/// Implemented by trigger data (<c>IAutomationTriggerData</c>) that describes itself in the <c>Write to log</c> step
/// (<i>Include trigger details</i>). Without it, the step logs the trigger identifier only.
/// </summary>
/// <remarks>
/// The text is written to the application log for every contact reaching the step. Return identifiers and non-personal
/// values only (IDs, code names, counts, amounts, dates) – never names, emails, addresses, or free text entered by visitors.
/// Use invariant culture for numbers and dates.
/// </remarks>
public interface ITriggerDataDescription
{
    /// <summary>
    /// Returns a short description of the trigger data, for example <c>order 42, customer 7</c>.
    /// </summary>
    public string Describe();
}
