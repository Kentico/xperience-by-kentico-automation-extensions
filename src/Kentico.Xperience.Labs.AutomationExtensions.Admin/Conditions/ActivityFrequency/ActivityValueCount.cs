namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Number of activities sharing one column value.
/// </summary>
/// <param name="Value">Column value; <c>null</c> for activities without it.</param>
/// <param name="Count">Number of activities with the value.</param>
public sealed record ActivityValueCount(string? Value, int Count);
