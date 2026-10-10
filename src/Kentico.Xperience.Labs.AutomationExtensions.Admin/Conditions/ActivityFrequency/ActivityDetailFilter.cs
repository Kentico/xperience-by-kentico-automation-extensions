namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Activity detail a "contains" filter of <see cref="ActivityFrequencyCondition"/> searches.
/// </summary>
public enum ActivityDetailFilter
{
    /// <summary>Page URL (<c>ActivityURL</c>) – never the referrer.</summary>
    Url,

    /// <summary>Activity title (<c>ActivityTitle</c>), e.g. form name.</summary>
    Title,

    /// <summary>Activity value (<c>ActivityValue</c>), e.g. clicked link URL or custom activity value.</summary>
    Value
}
