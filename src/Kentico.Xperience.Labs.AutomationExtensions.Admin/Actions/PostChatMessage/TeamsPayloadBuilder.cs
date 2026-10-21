using System.Text.Json.Nodes;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Builds the JSON body of a Microsoft Teams Workflows webhook request ("When a Teams webhook request is received"):
/// a <c>message</c> with one Adaptive Card attachment (title, text, fact set, one open-URL action per link).
/// </summary>
/// <remarks>
/// Envelope (<c>type</c>, <c>attachments[].contentType</c>, <c>contentUrl: null</c>, <c>content</c>) from the trigger's
/// request body schema: https://learn.microsoft.com/en-us/connectors/teams/#microsoft-teams-webhook.
/// Teams needs <c>\n\n</c> for a line break in a TextBlock:
/// https://learn.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/cards-format#newlines-for-adaptive-cards.
/// Teams renders a Markdown subset in TextBlocks and facts, so <c>**</c> or <c>_</c> in user text format it.
/// Texts are shortened (same limits as Slack) to stay well below the 28 KB Teams message limit.
/// </remarks>
internal static class TeamsPayloadBuilder
{
    /// <summary>
    /// Adaptive Card schema version. Every element used (TextBlock, FactSet, Action.OpenUrl) exists since 1.0;
    /// Microsoft's webhook samples use 1.0 – 1.2, Teams renders newer versions too.
    /// </summary>
    public const string ADAPTIVE_CARD_VERSION = "1.4";

    public const string ADAPTIVE_CARD_CONTENT_TYPE = "application/vnd.microsoft.card.adaptive";
    public const int TITLE_MAX_LENGTH = 150;
    public const int TEXT_MAX_LENGTH = 3000;
    public const int FACT_VALUE_MAX_LENGTH = 2000;


    /// <summary>
    /// Returns the JSON body for <paramref name="message"/>.
    /// </summary>
    public static string Build(ChatMessage message)
    {
        string title = ChatText.NormalizeLineBreaks(message.Title).Trim().Replace('\n', ' ');
        string text = ChatText.NormalizeLineBreaks(message.Text).Trim();
        var body = new JsonArray();

        if (title.Length > 0)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = ChatText.Truncate(title, TITLE_MAX_LENGTH),
                ["weight"] = "Bolder",
                ["size"] = "Medium",
                ["wrap"] = true
            });
        }

        if (text.Length > 0)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = ChatText.Truncate(text, TEXT_MAX_LENGTH).Replace("\n", "\n\n", StringComparison.Ordinal),
                ["wrap"] = true
            });
        }

        var facts = new JsonArray();
        foreach (var (name, value) in message.Facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)))
        {
            facts.Add(new JsonObject
            {
                ["title"] = name,
                ["value"] = ChatText.Truncate(ChatText.NormalizeLineBreaks(value).Replace('\n', ' '), FACT_VALUE_MAX_LENGTH)
            });
        }

        if (facts.Count > 0)
        {
            body.Add(new JsonObject { ["type"] = "FactSet", ["facts"] = facts });
        }

        var card = new JsonObject
        {
            ["$schema"] = "https://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = ADAPTIVE_CARD_VERSION,
            ["body"] = body
        };

        if (message.LinkList.Count > 0)
        {
            var actions = new JsonArray();
            foreach (var link in message.LinkList)
            {
                actions.Add(new JsonObject
                {
                    ["type"] = "Action.OpenUrl",
                    ["title"] = link.Label,
                    ["url"] = link.Url.AbsoluteUri
                });
            }

            card["actions"] = actions;
        }

        var payload = new JsonObject
        {
            ["type"] = "message",
            ["attachments"] = new JsonArray
            {
                new JsonObject
                {
                    ["contentType"] = ADAPTIVE_CARD_CONTENT_TYPE,
                    ["contentUrl"] = null,
                    ["content"] = card
                }
            }
        };

        return payload.ToJsonString();
    }
}
