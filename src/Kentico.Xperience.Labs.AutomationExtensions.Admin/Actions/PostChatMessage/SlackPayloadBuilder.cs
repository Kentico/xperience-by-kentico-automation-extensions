using System.Text.Json.Nodes;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Builds the JSON body of a Slack incoming webhook request: a plain-text fallback (<c>text</c>, used in notifications)
/// and Block Kit <c>blocks</c> (header, text, fields, link).
/// </summary>
/// <remarks>
/// Limits from the Block Kit reference (https://docs.slack.dev/reference/block-kit/blocks/): header text 150 characters,
/// section text 3000, at most 10 fields of 2000 characters each. Longer texts are shortened with "…".
/// User text is escaped (<c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>), other mrkdwn characters (<c>*</c>, <c>_</c>, <c>~</c>)
/// are kept and format the text. Admin links are mrkdwn links in one line, not buttons: link buttons send an interaction payload
/// that an incoming webhook can't acknowledge.
/// </remarks>
internal static class SlackPayloadBuilder
{
    public const int HEADER_MAX_LENGTH = 150;
    public const int SECTION_TEXT_MAX_LENGTH = 3000;
    public const int FIELD_TEXT_MAX_LENGTH = 2000;
    public const int MAX_FIELDS = 10;

    /// <summary>
    /// Separator between links in the link line.
    /// </summary>
    public const string LINK_SEPARATOR = " · ";

    /// <summary>
    /// Fallback text used when the message has neither title nor text (Slack requires a non-empty <c>text</c>).
    /// </summary>
    public const string EMPTY_FALLBACK_TEXT = "Message from Xperience automation";


    /// <summary>
    /// Returns the JSON body for <paramref name="message"/>.
    /// </summary>
    public static string Build(ChatMessage message)
    {
        string title = ChatText.NormalizeLineBreaks(message.Title).Trim();
        string text = ChatText.NormalizeLineBreaks(message.Text).Trim();
        var blocks = new JsonArray();

        if (title.Length > 0)
        {
            // Header is plain_text: shown as typed, no escaping. Line breaks aren't allowed in headers.
            blocks.Add(new JsonObject
            {
                ["type"] = "header",
                ["text"] = PlainText(ChatText.Truncate(title.Replace('\n', ' '), HEADER_MAX_LENGTH))
            });
        }

        if (text.Length > 0)
        {
            blocks.Add(new JsonObject
            {
                ["type"] = "section",
                ["text"] = Mrkdwn(ChatText.EscapeSlackAndTruncate(text, SECTION_TEXT_MAX_LENGTH))
            });
        }

        var fields = new JsonArray();
        foreach (var (name, value) in message.Facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).Take(MAX_FIELDS))
        {
            string label = $"*{ChatText.EscapeSlackAndTruncate(name, 100)}*\n";
            fields.Add(Mrkdwn(label + ChatText.EscapeSlackAndTruncate(ChatText.NormalizeLineBreaks(value), FIELD_TEXT_MAX_LENGTH - label.Length)));
        }

        if (fields.Count > 0)
        {
            blocks.Add(new JsonObject { ["type"] = "section", ["fields"] = fields });
        }

        if (message.LinkList.Count > 0)
        {
            // All links in one line: <url|label> · <url|label>
            string links = string.Join(LINK_SEPARATOR, message.LinkList.Select(link =>
                $"<{ChatText.EscapeSlackAndTruncate(link.Url.AbsoluteUri, SECTION_TEXT_MAX_LENGTH)}|{ChatText.EscapeSlackAndTruncate(link.Label, 75)}>"));

            blocks.Add(new JsonObject
            {
                ["type"] = "section",
                ["text"] = Mrkdwn(links)
            });
        }

        string fallback = string.Join("\n", new[] { title, text }.Where(part => part.Length > 0));

        var payload = new JsonObject
        {
            ["text"] = fallback.Length > 0 ? ChatText.EscapeSlackAndTruncate(fallback, SECTION_TEXT_MAX_LENGTH) : EMPTY_FALLBACK_TEXT,
            ["blocks"] = blocks
        };

        return payload.ToJsonString();
    }


    private static JsonObject PlainText(string text) => new() { ["type"] = "plain_text", ["text"] = text };


    private static JsonObject Mrkdwn(string text) => new() { ["type"] = "mrkdwn", ["text"] = text };
}
