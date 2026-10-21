using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Personal contact fields added to a chat message when <see cref="PostChatMessageActionProperties.IncludeContactDetails"/> is on.
/// </summary>
internal sealed record ChatContactDetails(string? Email, string? FirstName, string? LastName, string? CompanyName);


/// <summary>
/// Builds the <see cref="ChatMessage"/> of <see cref="PostChatMessageAction"/> from step properties, independent of the platform.
/// </summary>
internal static class ChatMessageFactory
{
    /// <summary>
    /// Resolves tokens in title and message (same tokens as <c>Set contact field</c>) and adds the facts and links.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="tokens">Token values for the contact.</param>
    /// <param name="contactDetails">Contact fields to add, or <c>null</c> to add none.</param>
    /// <param name="contactLink">Link to the contact in the admin, or <c>null</c>.</param>
    /// <param name="formSubmissionLink">Link to the form submission that started the process, or <c>null</c>.</param>
    /// <param name="processLink">Link to the Contacts tab of the automation process, or <c>null</c>.</param>
    /// <param name="unresolvedTokens">Receives tokens replaced by empty text.</param>
    public static ChatMessage Create(
        PostChatMessageActionProperties properties,
        ContactFieldTokenValues tokens,
        ChatContactDetails? contactDetails,
        Uri? contactLink,
        Uri? formSubmissionLink,
        Uri? processLink,
        ICollection<string> unresolvedTokens)
    {
        string title = ContactFieldValueTemplate.Resolve(properties.Title, tokens, unresolvedTokens);
        string text = ContactFieldValueTemplate.Resolve(properties.Message, tokens, unresolvedTokens);

        var facts = new List<(string Name, string Value)> { (ChatMessage.FACT_PROCESS, tokens.ProcessName) };

        if (contactDetails is not null)
        {
            string name = string.Join(" ", new[] { contactDetails.FirstName, contactDetails.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

            AddIfNotEmpty(facts, ChatMessage.FACT_EMAIL, contactDetails.Email);
            AddIfNotEmpty(facts, ChatMessage.FACT_NAME, name);
            AddIfNotEmpty(facts, ChatMessage.FACT_COMPANY, contactDetails.CompanyName);
        }

        var links = new List<ChatLink>();

        if (properties.IncludeAdminLink && contactLink is not null)
        {
            links.Add(new ChatLink(ChatMessage.LINK_CONTACT, contactLink));
        }

        if (properties.IncludeFormSubmissionLink && formSubmissionLink is not null)
        {
            links.Add(new ChatLink(ChatMessage.LINK_FORM_SUBMISSION, formSubmissionLink));
        }

        if (properties.IncludeProcessLink && processLink is not null)
        {
            links.Add(new ChatLink(ChatMessage.LINK_PROCESS_CONTACTS, processLink));
        }

        return new ChatMessage(title, text, facts, links);
    }


    /// <summary>
    /// Returns the admin link to the Contacts tab of the running automation process, or <c>null</c> when
    /// <see cref="PostChatMessageActionProperties.IncludeProcessLink"/> is off, <paramref name="adminBaseUrl"/> is empty or
    /// invalid (the process isn't looked up then), or not exactly one process has the display name.
    /// </summary>
    public static async Task<Uri?> GetProcessLink(
        PostChatMessageActionProperties properties,
        string? adminBaseUrl,
        string? processName,
        IAutomationProcessResolver processResolver,
        CancellationToken cancellationToken)
    {
        if (!properties.IncludeProcessLink || !AdminLink.IsValidBaseUrl(adminBaseUrl))
        {
            return null;
        }

        int? processId = await processResolver.FindIdByDisplayName(processName, cancellationToken);

        return processId is null ? null : AdminLink.ProcessContacts(adminBaseUrl, processId.Value);
    }


    /// <summary>
    /// Returns the admin link to the form submission that started the process, or <c>null</c> when the process wasn't
    /// started by the built-in form submission trigger, its trigger data is missing, or <paramref name="adminBaseUrl"/> is empty.
    /// </summary>
    /// <param name="adminBaseUrl">Configured admin root URL.</param>
    /// <param name="triggerIdentifier">Identifier of the trigger that started the process.</param>
    /// <param name="triggerData">Trigger data of the process.</param>
    public static Uri? GetFormSubmissionLink(string? adminBaseUrl, string? triggerIdentifier, object? triggerData) =>
        string.Equals(triggerIdentifier, DefaultTriggerIdentifiers.FORM_SUBMISSION, StringComparison.Ordinal)
        && triggerData is FormSubmissionTriggerData formSubmission
            ? AdminLink.FormSubmission(adminBaseUrl, formSubmission.BizFormId, formSubmission.BizFormItemId)
            : null;


    private static void AddIfNotEmpty(List<(string Name, string Value)> facts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            facts.Add((name, value.Trim()));
        }
    }
}
