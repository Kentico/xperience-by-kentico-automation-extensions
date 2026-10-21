using System.Globalization;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Result keys that link steps saving a <see cref="StepResult"/> with conditions reading it.
/// A key combines the source name and a result number: <c>&lt;source name&gt;.&lt;number&gt;</c> (e.g. <c>SendWebhook.1</c>),
/// so different step types never overwrite each other's results.
/// </summary>
/// <remarks>
/// Keys are persisted in process data and must never change.
/// </remarks>
public static class StepResultKeys
{
    /// <summary>
    /// Number of results available per source.
    /// </summary>
    public const int MAXIMUM_RESULT_NUMBER = 5;


    /// <summary>
    /// Default result number.
    /// </summary>
    public const string DEFAULT_RESULT_NUMBER = "1";


    /// <summary>
    /// Result source of the library's "Send webhook" action.
    /// </summary>
    public static readonly StepResultSource SendWebhook = new(SendWebhookAction.NAME, "labs.automationextensions.sendwebhook.displayname");


    /// <summary>
    /// Result source of the library's "Set contact field (dynamic value)" action.
    /// </summary>
    public static readonly StepResultSource SetContactField = new(SetContactFieldAction.NAME, "labs.automationextensions.setcontactfield.displayname");


    /// <summary>
    /// Result source of the library's "Post to Teams / Slack" action.
    /// </summary>
    public static readonly StepResultSource PostChatMessage = new(PostChatMessageAction.NAME, "labs.automationextensions.postchatmessage.displayname");


    /// <summary>
    /// Returns the key for the given source name and result number, or <c>null</c> when either is missing or the number is out of range.
    /// </summary>
    /// <param name="sourceName">Name of a registered <see cref="StepResultSource"/>.</param>
    /// <param name="resultNumber">Result number (1 – <see cref="MAXIMUM_RESULT_NUMBER"/>) as selected in the result number drop-down.</param>
    public static string? Create(string? sourceName, string? resultNumber)
    {
        if (string.IsNullOrWhiteSpace(sourceName)
            || !int.TryParse(resultNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
            || number is < 1 or > MAXIMUM_RESULT_NUMBER)
        {
            return null;
        }

        return $"{sourceName}.{number.ToString(CultureInfo.InvariantCulture)}";
    }
}
