namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// Part of a URL a <see cref="UrlMatchRule"/> compares. Persisted in rule JSON by name – never rename members.
/// </summary>
public enum UrlPart
{
    /// <summary>Whole normalized URL: <c>scheme://host/path?query</c>, fragment removed, query as stored.</summary>
    Url,

    /// <summary>Host name, without port (e.g. <c>www.example.com</c>).</summary>
    Host,

    /// <summary>Percent-decoded path without query, trailing slash ignored (root = <c>/</c>).</summary>
    Path,

    /// <summary>Percent-decoded value(s) of one query parameter (<see cref="UrlMatchRule.Parameter"/>).</summary>
    QueryParameter
}
