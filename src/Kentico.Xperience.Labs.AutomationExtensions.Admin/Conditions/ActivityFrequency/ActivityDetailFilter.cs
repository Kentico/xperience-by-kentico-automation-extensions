namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Activity detail a filter of <see cref="ActivityFrequencyCondition"/> searches.
/// </summary>
public enum ActivityDetailFilter
{
    /// <summary>URL rules on the page URL (<c>ActivityURL</c>) or the clicked link of email clicks (<c>ActivityValue</c>) – never the referrer.</summary>
    Url,

    /// <summary>Activity title (<c>ActivityTitle</c>), e.g. form name.</summary>
    Title,

    /// <summary>Activity value (<c>ActivityValue</c>), e.g. clicked link URL or custom activity value.</summary>
    Value,

    /// <summary>Content tree page the activity was logged on (<c>ActivityWebPageItemGUID</c>).</summary>
    Page,

    /// <summary>Content language the activity was logged in (<c>ActivityLanguageID</c>).</summary>
    Language
}
