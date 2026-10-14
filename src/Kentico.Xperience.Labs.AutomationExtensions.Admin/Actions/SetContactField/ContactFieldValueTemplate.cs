using System.Globalization;
using System.Text.RegularExpressions;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Resolves <c>{token}</c> and <c>{token.Part}</c> placeholders (case-insensitive) in value templates and operands of
/// <see cref="SetContactFieldAction"/>. Unknown or unresolvable tokens become empty text and are reported.
/// </summary>
/// <remarks>
/// Text in braces that doesn't look like a token (e.g. <c>{a b}</c> or <c>{}</c>) is kept as typed. There's no escaping –
/// a literal <c>{word}</c> can't be written.
/// </remarks>
internal static partial class ContactFieldValueTemplate
{
    public const string NOW = "now";
    public const string TODAY = "today";
    public const string PROCESS_NAME = LogMessageFormatter.PROCESS_NAME;
    public const string TRIGGER_IDENTIFIER = LogMessageFormatter.TRIGGER_IDENTIFIER;
    public const string CONTACT_ID = LogMessageFormatter.CONTACT_ID;
    public const string CONTACT_GUID = LogMessageFormatter.CONTACT_GUID;
    public const string CONTACT = "contact";
    public const string TRIGGER_DATA = "triggerData";


    /// <summary>
    /// Returns <paramref name="template"/> with tokens replaced by their values. Each token that couldn't be resolved
    /// is replaced by empty text and added (as typed, without braces) to <paramref name="unresolvedTokens"/>.
    /// </summary>
    public static string Resolve(string? template, ContactFieldTokenValues values, ICollection<string> unresolvedTokens)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return TokenPattern().Replace(template, match =>
        {
            string name = match.Groups["name"].Value;
            string? part = match.Groups["part"].Success ? match.Groups["part"].Value : null;
            string? value = values.Resolve(name, part);
            if (value is null)
            {
                unresolvedTokens.Add(match.Value[1..^1]);
                return string.Empty;
            }

            return value;
        });
    }


    [GeneratedRegex(@"\{(?<name>[A-Za-z]+)(?:\.(?<part>[A-Za-z_][A-Za-z0-9_]*))?\}")]
    private static partial Regex TokenPattern();
}


/// <summary>
/// Values available to <see cref="ContactFieldValueTemplate"/> for one contact.
/// </summary>
internal sealed class ContactFieldTokenValues
{
    /// <summary>
    /// Server-local current date and time (Xperience stores contact dates in server-local time).
    /// </summary>
    public DateTime Now { get; init; }


    public string ProcessName { get; init; } = string.Empty;


    /// <summary>
    /// Identifier of the trigger that started the process, or empty.
    /// </summary>
    public string TriggerIdentifier { get; init; } = string.Empty;


    public int ContactId { get; init; }


    public Guid ContactGuid { get; init; }


    /// <summary>
    /// Returns the formatted value of a readable contact field, or <c>null</c> when the field doesn't exist.
    /// </summary>
    public Func<string, string?> GetContactFieldValue { get; init; } = _ => null;


    /// <summary>
    /// Registered trigger that started the process (<see cref="RegisteredTriggers.Find"/>), or <c>null</c> when it isn't
    /// registered. <c>{triggerData.X}</c> tokens resolve only for data of this trigger's data type.
    /// </summary>
    public RegisteredTrigger? Trigger { get; init; }


    /// <summary>
    /// Trigger data of the process (<c>null</c> when the trigger provides none).
    /// </summary>
    public object? TriggerData { get; init; }


    /// <summary>
    /// Returns the value of token <paramref name="name"/> (with optional <paramref name="part"/>), or <c>null</c> when unknown.
    /// </summary>
    public string? Resolve(string name, string? part)
    {
        if (part is not null)
        {
            if (Is(name, ContactFieldValueTemplate.CONTACT))
            {
                return GetContactFieldValue(part);
            }

            if (Is(name, ContactFieldValueTemplate.TRIGGER_DATA))
            {
                return TriggerDataTokens.TryGetValue(Trigger, TriggerData, part, out string value) ? value : null;
            }

            return null;
        }

        if (Is(name, ContactFieldValueTemplate.NOW))
        {
            return Now.ToString(ContactFieldValueConverter.DATE_TIME_FORMAT, CultureInfo.InvariantCulture);
        }

        if (Is(name, ContactFieldValueTemplate.TODAY))
        {
            return Now.ToString(ContactFieldValueConverter.DATE_FORMAT, CultureInfo.InvariantCulture);
        }

        if (Is(name, ContactFieldValueTemplate.PROCESS_NAME))
        {
            return ProcessName;
        }

        if (Is(name, ContactFieldValueTemplate.TRIGGER_IDENTIFIER))
        {
            return TriggerIdentifier;
        }

        if (Is(name, ContactFieldValueTemplate.CONTACT_ID))
        {
            return ContactId.ToString(CultureInfo.InvariantCulture);
        }

        return Is(name, ContactFieldValueTemplate.CONTACT_GUID) ? ContactGuid.ToString("D") : null;
    }


    private static bool Is(string name, string token) => string.Equals(name, token, StringComparison.OrdinalIgnoreCase);
}
