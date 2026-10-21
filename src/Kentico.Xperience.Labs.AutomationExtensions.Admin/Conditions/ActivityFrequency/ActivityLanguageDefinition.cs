namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Content language available to the language filter of <see cref="ActivityFrequencyCondition"/>.
/// </summary>
/// <param name="Name">Content language code name (stored in step properties – stable across environments, unlike the ID).</param>
/// <param name="DisplayName">Display name; localized when returned by <see cref="ActivityLanguageResolver.GetLanguages(CancellationToken)"/>.</param>
/// <param name="Id">Content language ID (stored in the <c>ActivityLanguageID</c> column of logged activities).</param>
public sealed record ActivityLanguageDefinition(string Name, string DisplayName, int Id);
