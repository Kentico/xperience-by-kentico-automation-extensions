namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// "Activity was logged on one of these content tree pages" filter (<c>ActivityWebPageItemGUID</c>) applied when counting activities.
/// </summary>
/// <param name="WebPageGuids">Distinct, non-empty web page item GUIDs; any of them matches.</param>
public sealed record ActivityPageCriterion(IReadOnlyList<Guid> WebPageGuids) : ActivityCriterion;
