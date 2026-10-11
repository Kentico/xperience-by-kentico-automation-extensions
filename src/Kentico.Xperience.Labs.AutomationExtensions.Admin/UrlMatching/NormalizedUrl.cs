using System.Text.RegularExpressions;

namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// URL split into the parts rules compare (<see cref="UrlPart"/>), normalized so URL variants match consistently.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Fragment (<c>#…</c>) is always removed; default ports are dropped; scheme and host are lowercase.</item>
/// <item>Path: percent-decoded, without query, trailing slash removed except for the root; empty path = <c>/</c>.</item>
/// <item>Query parameters: <c>+</c> = space, names and values percent-decoded; a parameter without <c>=</c> has an empty value.</item>
/// <item>Url: <c>scheme://host[:port]/path?query</c> with the normalized path and the query as stored.</item>
/// <item>Relative URLs (starting with <c>/</c> or <c>?</c>) have a path and query but no host. Anything else
/// (empty, <c>mailto:</c>, malformed) isn't recognized – it has no parts.</item>
/// </list>
/// </remarks>
internal sealed partial class NormalizedUrl
{
    private static readonly NormalizedUrl unrecognized = new(false, null, null, null, []);


    /// <summary>Whether the URL could be parsed. Unrecognized URLs never match positive rules.</summary>
    public bool IsRecognized { get; }

    /// <summary>Normalized whole URL; <c>null</c> when unrecognized.</summary>
    public string? Url { get; }

    /// <summary>Lowercase host; <c>null</c> for relative or unrecognized URLs.</summary>
    public string? Host { get; }

    /// <summary>Normalized path; <c>null</c> when unrecognized.</summary>
    public string? Path { get; }

    /// <summary>Decoded query parameters in stored order (names may repeat).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> QueryParameters { get; }


    private NormalizedUrl(bool isRecognized, string? url, string? host, string? path, IReadOnlyList<KeyValuePair<string, string>> queryParameters)
    {
        IsRecognized = isRecognized;
        Url = url;
        Host = host;
        Path = path;
        QueryParameters = queryParameters;
    }


    /// <summary>
    /// Parses a stored URL (absolute or relative). Never throws.
    /// </summary>
    public static NormalizedUrl Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return unrecognized;
        }

        string text = value.Trim();
        int fragmentStart = text.IndexOf('#');
        if (fragmentStart >= 0)
        {
            text = text[..fragmentStart];
        }

        if (text.StartsWith("//", StringComparison.Ordinal))
        {
            // Protocol-relative: parse with a placeholder scheme, keep it out of the normalized URL.
            return ParseAbsolute("https:" + text, includeScheme: false);
        }

        if (AbsoluteUrlPattern().IsMatch(text))
        {
            return ParseAbsolute(text, includeScheme: true);
        }

        if (text.StartsWith('/') || text.StartsWith('?'))
        {
            int queryStart = text.IndexOf('?');
            string rawPath = queryStart >= 0 ? text[..queryStart] : text;
            string query = queryStart >= 0 ? text[queryStart..] : string.Empty;
            string path = NormalizePath(Uri.UnescapeDataString(rawPath));

            return new NormalizedUrl(true, path + NormalizeQueryText(query), null, path, ParseQuery(query));
        }

        return unrecognized;
    }


    /// <summary>
    /// Normalizes a path: trailing slash removed except for the root, empty = <c>/</c>.
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (path.Length == 0)
        {
            return "/";
        }

        string trimmed = path.TrimEnd('/');

        return trimmed.Length == 0 ? "/" : trimmed;
    }


    /// <summary>
    /// Percent-decodes a query string component (<c>+</c> = space). Malformed escapes are kept as they are.
    /// </summary>
    public static string DecodeQueryComponent(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));


    private static NormalizedUrl ParseAbsolute(string text, bool includeScheme)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return unrecognized;
        }

        string host = uri.Host.ToLowerInvariant();
        string path = NormalizePath(Uri.UnescapeDataString(uri.AbsolutePath));
        string query = NormalizeQueryText(uri.Query);
        string authority = uri.IsDefaultPort ? host : $"{host}:{uri.Port}";
        string prefix = includeScheme ? $"{uri.Scheme.ToLowerInvariant()}://" : "//";

        return new NormalizedUrl(true, $"{prefix}{authority}{path}{query}", host, path, ParseQuery(query));
    }


    /// <summary>Query as stored; a lone <c>?</c> counts as no query.</summary>
    private static string NormalizeQueryText(string query) => query == "?" ? string.Empty : query;


    private static List<KeyValuePair<string, string>> ParseQuery(string query)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        if (query.Length <= 1)
        {
            return parameters;
        }

        foreach (string pair in query[1..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = pair.IndexOf('=');
            string name = DecodeQueryComponent(separator >= 0 ? pair[..separator] : pair);
            string value = separator >= 0 ? DecodeQueryComponent(pair[(separator + 1)..]) : string.Empty;
            if (name.Length > 0)
            {
                parameters.Add(new KeyValuePair<string, string>(name, value));
            }
        }

        return parameters;
    }


    [GeneratedRegex("^[A-Za-z][A-Za-z0-9+.-]*://", RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteUrlPattern();
}
