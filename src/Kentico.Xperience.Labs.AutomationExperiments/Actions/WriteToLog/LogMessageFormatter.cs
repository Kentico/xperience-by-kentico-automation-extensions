using System.Text.RegularExpressions;

namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;

/// <summary>
/// Replaces <c>{placeholder}</c> tokens in marketer-written log messages. Unknown placeholders are kept unchanged.
/// </summary>
public static partial class LogMessageFormatter
{
    public const string PROCESS_NAME = "processName";
    public const string CONTACT_ID = "contactId";
    public const string CONTACT_GUID = "contactGuid";
    public const string TRIGGER_IDENTIFIER = "triggerIdentifier";


    /// <summary>
    /// Returns <paramref name="template"/> with known placeholders (case-insensitive) replaced by their values.
    /// </summary>
    public static string Format(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var lookup = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

        return PlaceholderPattern().Replace(template, match =>
            lookup.TryGetValue(match.Groups["name"].Value, out string? value) ? value : match.Value);
    }


    [GeneratedRegex(@"\{(?<name>[A-Za-z]+)\}")]
    private static partial Regex PlaceholderPattern();
}
