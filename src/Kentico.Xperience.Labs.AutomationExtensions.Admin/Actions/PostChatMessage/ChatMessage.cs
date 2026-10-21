namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Provider-independent content of a chat message, converted to JSON by <see cref="SlackPayloadBuilder"/> or
/// <see cref="TeamsPayloadBuilder"/>. All texts are plain text (tokens already resolved).
/// </summary>
/// <param name="Title">Optional title. Empty – no title.</param>
/// <param name="Text">Message text. Line breaks are kept.</param>
/// <param name="Facts">Label/value pairs shown below the text. Labels are plain English (messages leave the admin, no culture).</param>
/// <param name="Links">Links to admin pages (contact, form submission, process contacts), in display order. <c>null</c> – no links.</param>
public sealed record ChatMessage(
    string Title,
    string Text,
    IReadOnlyList<(string Name, string Value)> Facts,
    IReadOnlyList<ChatLink>? Links = null)
{
    public const string FACT_PROCESS = "Process";
    public const string FACT_EMAIL = "Email";
    public const string FACT_NAME = "Name";
    public const string FACT_COMPANY = "Company";

    public const string LINK_CONTACT = "Open contact";
    public const string LINK_FORM_SUBMISSION = "Open form submission";
    public const string LINK_PROCESS_CONTACTS = "Open process contacts";


    /// <summary>
    /// <see cref="Links"/>, or empty.
    /// </summary>
    public IReadOnlyList<ChatLink> LinkList => Links ?? [];
}


/// <summary>
/// Link shown in a chat message.
/// </summary>
/// <param name="Label">Plain English label (e.g. <see cref="ChatMessage.LINK_CONTACT"/>).</param>
/// <param name="Url">Absolute URL.</param>
public sealed record ChatLink(string Label, Uri Url);
