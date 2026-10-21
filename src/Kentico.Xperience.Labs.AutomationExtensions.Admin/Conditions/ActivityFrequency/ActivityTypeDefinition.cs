namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Activity type available to the <see cref="ActivityFrequencyCondition"/>.
/// </summary>
/// <param name="Name">Activity type code name (stored in the <c>ActivityType</c> column of logged activities).</param>
/// <param name="DisplayName">Display name; localized when returned by <see cref="ActivityTypeResolver.GetEnabledTypes(CancellationToken)"/>.</param>
/// <param name="Enabled">Whether the activity type is enabled (new activities are logged).</param>
public sealed record ActivityTypeDefinition(string Name, string DisplayName, bool Enabled);
