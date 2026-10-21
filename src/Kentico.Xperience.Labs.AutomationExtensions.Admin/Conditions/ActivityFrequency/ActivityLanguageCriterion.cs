namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// "Activity was logged on this content language" filter (<c>ActivityLanguageID</c>) applied when counting activities.
/// </summary>
/// <param name="LanguageId">Content language ID resolved from the stored language code name.</param>
public sealed record ActivityLanguageCriterion(int LanguageId) : ActivityCriterion;
