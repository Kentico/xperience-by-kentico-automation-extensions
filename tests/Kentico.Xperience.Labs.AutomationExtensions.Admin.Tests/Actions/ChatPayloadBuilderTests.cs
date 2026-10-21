using System.Text.Json;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ChatPayloadBuilderTests
{
    private static readonly Uri link = new("https://example.test/admin/contact-management/contacts/42/overview");
    private static readonly Uri formLink = new("https://example.test/admin/forms/list/2/submissions/15");
    private static readonly Uri processLink = new("https://example.test/admin/automation/list/4002/automation-contacts");


    [Test]
    public void Slack_FullMessage_BuildsHeaderTextFieldsAndLink()
    {
        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(CreateMessage()));
        var root = document.RootElement;
        var blocks = root.GetProperty("blocks").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("text").GetString(), Is.EqualTo("Hot lead\nJane is ready to buy."));
            Assert.That(blocks.Select(block => block.GetProperty("type").GetString()), Is.EqualTo(new[] { "header", "section", "section", "section" }));
            Assert.That(blocks[0].GetProperty("text").GetProperty("type").GetString(), Is.EqualTo("plain_text"));
            Assert.That(blocks[0].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo("Hot lead"));
            Assert.That(blocks[1].GetProperty("text").GetProperty("type").GetString(), Is.EqualTo("mrkdwn"));
            Assert.That(blocks[1].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo("Jane is ready to buy."));
            Assert.That(blocks[2].GetProperty("fields").EnumerateArray().Select(field => field.GetProperty("text").GetString()),
                Is.EqualTo(new[] { "*Process*\nLead nurture", "*Email*\njane@example.test" }));
            Assert.That(blocks[3].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo($"<{link.AbsoluteUri}|Open contact>"));
        });
    }


    [Test]
    public void Slack_EscapesControlCharactersInUserText()
    {
        var message = new ChatMessage("A & B", "<b>Tom & Jerry</b>", [("Company", "R&D <Labs>")]);

        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(message));
        var blocks = document.RootElement.GetProperty("blocks").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("text").GetString(), Is.EqualTo("A &amp; B\n&lt;b&gt;Tom &amp; Jerry&lt;/b&gt;"));
            Assert.That(blocks[0].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo("A & B"), "Header is plain_text.");
            Assert.That(blocks[1].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo("&lt;b&gt;Tom &amp; Jerry&lt;/b&gt;"));
            Assert.That(blocks[2].GetProperty("fields")[0].GetProperty("text").GetString(), Is.EqualTo("*Company*\nR&amp;D &lt;Labs&gt;"));
        });
    }


    [Test]
    public void Slack_LongTexts_TruncatedToBlockLimits()
    {
        var message = new ChatMessage(new string('t', 200), new string('&', 2000), [("Process", new string('v', 3000))]);

        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(message));
        var blocks = document.RootElement.GetProperty("blocks").EnumerateArray().ToList();
        string header = blocks[0].GetProperty("text").GetProperty("text").GetString()!;
        string text = blocks[1].GetProperty("text").GetProperty("text").GetString()!;
        string field = blocks[2].GetProperty("fields")[0].GetProperty("text").GetString()!;

        Assert.Multiple(() =>
        {
            Assert.That(header, Has.Length.EqualTo(SlackPayloadBuilder.HEADER_MAX_LENGTH).And.EndsWith("…"));
            Assert.That(text, Has.Length.LessThanOrEqualTo(SlackPayloadBuilder.SECTION_TEXT_MAX_LENGTH).And.EndsWith("&amp;…"));
            Assert.That(text.Replace("&amp;", "", StringComparison.Ordinal), Is.EqualTo("…"), "Entities aren't cut.");
            Assert.That(field, Has.Length.EqualTo(SlackPayloadBuilder.FIELD_TEXT_MAX_LENGTH).And.EndsWith("…"));
            Assert.That(document.RootElement.GetProperty("text").GetString(), Has.Length.LessThanOrEqualTo(SlackPayloadBuilder.SECTION_TEXT_MAX_LENGTH));
        });
    }


    [Test]
    public void Slack_MoreThanTenFacts_FieldsLimitedToTen()
    {
        var facts = Enumerable.Range(1, 12).Select(i => ($"Fact {i}", "value")).ToList();

        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(new ChatMessage("", "Text", facts)));

        Assert.That(document.RootElement.GetProperty("blocks")[1].GetProperty("fields").GetArrayLength(), Is.EqualTo(SlackPayloadBuilder.MAX_FIELDS));
    }


    [Test]
    public void Slack_EmptyTitleFactsAndLink_OnlyTextSection()
    {
        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(new ChatMessage(" ", "Line 1\r\nLine 2", [("Email", " ")])));
        var blocks = document.RootElement.GetProperty("blocks").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Count.EqualTo(1));
            Assert.That(blocks[0].GetProperty("text").GetProperty("text").GetString(), Is.EqualTo("Line 1\nLine 2"));
            Assert.That(document.RootElement.GetProperty("text").GetString(), Is.EqualTo("Line 1\nLine 2"));
        });
    }


    [Test]
    public void Slack_EmptyTitleAndText_UsesFallbackText()
    {
        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(new ChatMessage("", "", [("Process", "Lead nurture")])));

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("text").GetString(), Is.EqualTo(SlackPayloadBuilder.EMPTY_FALLBACK_TEXT));
            Assert.That(document.RootElement.GetProperty("blocks").GetArrayLength(), Is.EqualTo(1));
        });
    }


    [Test]
    public void Teams_FullMessage_BuildsAdaptiveCardEnvelope()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(CreateMessage()));
        var root = document.RootElement;
        var attachment = root.GetProperty("attachments").EnumerateArray().Single();
        var card = attachment.GetProperty("content");
        var body = card.GetProperty("body").EnumerateArray().ToList();
        var action = card.GetProperty("actions").EnumerateArray().Single();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("message"));
            Assert.That(attachment.GetProperty("contentType").GetString(), Is.EqualTo("application/vnd.microsoft.card.adaptive"));
            Assert.That(attachment.GetProperty("contentUrl").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(card.GetProperty("type").GetString(), Is.EqualTo("AdaptiveCard"));
            Assert.That(card.GetProperty("version").GetString(), Is.EqualTo(TeamsPayloadBuilder.ADAPTIVE_CARD_VERSION));
            Assert.That(body.Select(element => element.GetProperty("type").GetString()), Is.EqualTo(new[] { "TextBlock", "TextBlock", "FactSet" }));
            Assert.That(body[0].GetProperty("text").GetString(), Is.EqualTo("Hot lead"));
            Assert.That(body[0].GetProperty("weight").GetString(), Is.EqualTo("Bolder"));
            Assert.That(body[0].GetProperty("wrap").GetBoolean(), Is.True);
            Assert.That(body[1].GetProperty("text").GetString(), Is.EqualTo("Jane is ready to buy."));
            Assert.That(body[1].GetProperty("wrap").GetBoolean(), Is.True);
            Assert.That(body[2].GetProperty("facts").EnumerateArray().Select(fact => $"{fact.GetProperty("title").GetString()}={fact.GetProperty("value").GetString()}"),
                Is.EqualTo(new[] { "Process=Lead nurture", "Email=jane@example.test" }));
            Assert.That(action.GetProperty("type").GetString(), Is.EqualTo("Action.OpenUrl"));
            Assert.That(action.GetProperty("title").GetString(), Is.EqualTo(ChatMessage.LINK_CONTACT));
            Assert.That(action.GetProperty("url").GetString(), Is.EqualTo(link.AbsoluteUri));
        });
    }


    [Test]
    public void Teams_LineBreaks_DoubledForTextBlock()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(new ChatMessage("", "Line 1\r\nLine 2\nLine 3", [])));

        string? text = document.RootElement.GetProperty("attachments")[0].GetProperty("content").GetProperty("body")[0].GetProperty("text").GetString();

        Assert.That(text, Is.EqualTo("Line 1\n\nLine 2\n\nLine 3"));
    }


    [Test]
    public void Slack_ThreeLinks_OneLineInOrder()
    {
        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(new ChatMessage("", "Text", [], CreateThreeLinks())));
        var blocks = document.RootElement.GetProperty("blocks").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(blocks, Has.Count.EqualTo(2));
            Assert.That(blocks[1].GetProperty("text").GetProperty("text").GetString(),
                Is.EqualTo($"<{link.AbsoluteUri}|Open contact> · <{formLink.AbsoluteUri}|Open form submission> · <{processLink.AbsoluteUri}|Open process contacts>"));
        });
    }


    [Test]
    public void Slack_EmptyLinkList_NoLinkSection()
    {
        using var document = JsonDocument.Parse(SlackPayloadBuilder.Build(new ChatMessage("", "Text", [], [])));

        Assert.That(document.RootElement.GetProperty("blocks").GetArrayLength(), Is.EqualTo(1));
    }


    [Test]
    public void Teams_ThreeLinks_OneOpenUrlActionEach()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(new ChatMessage("", "Text", [], CreateThreeLinks())));
        var actions = document.RootElement.GetProperty("attachments")[0].GetProperty("content").GetProperty("actions").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(actions.Select(action => action.GetProperty("type").GetString()), Is.All.EqualTo("Action.OpenUrl"));
            Assert.That(actions.Select(action => action.GetProperty("title").GetString()), Is.EqualTo(new[] { "Open contact", "Open form submission", "Open process contacts" }));
            Assert.That(actions.Select(action => action.GetProperty("url").GetString()), Is.EqualTo(new[] { link.AbsoluteUri, formLink.AbsoluteUri, processLink.AbsoluteUri }));
        });
    }


    [Test]
    public void Teams_EmptyLinkList_NoActions()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(new ChatMessage("", "Text", [], [])));

        Assert.That(document.RootElement.GetProperty("attachments")[0].GetProperty("content").TryGetProperty("actions", out _), Is.False);
    }


    [Test]
    public void Teams_EmptyTitleFactsAndNoLink_Omitted()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(new ChatMessage("", "Text", [("Email", "")])));
        var card = document.RootElement.GetProperty("attachments")[0].GetProperty("content");

        Assert.Multiple(() =>
        {
            Assert.That(card.GetProperty("body").GetArrayLength(), Is.EqualTo(1));
            Assert.That(card.TryGetProperty("actions", out _), Is.False);
        });
    }


    [Test]
    public void Teams_LongTexts_Truncated()
    {
        using var document = JsonDocument.Parse(TeamsPayloadBuilder.Build(new ChatMessage(new string('t', 200), new string('x', 5000), [])));
        var body = document.RootElement.GetProperty("attachments")[0].GetProperty("content").GetProperty("body");

        Assert.Multiple(() =>
        {
            Assert.That(body[0].GetProperty("text").GetString(), Has.Length.EqualTo(TeamsPayloadBuilder.TITLE_MAX_LENGTH).And.EndsWith("…"));
            Assert.That(body[1].GetProperty("text").GetString(), Has.Length.EqualTo(TeamsPayloadBuilder.TEXT_MAX_LENGTH).And.EndsWith("…"));
        });
    }


    [Test]
    public void Truncate_DoesNotSplitSurrogatePairs()
    {
        string text = string.Concat(Enumerable.Repeat("😀", 10));

        string result = ChatText.Truncate(text, 6);

        Assert.That(result, Is.EqualTo("😀😀…"));
    }


    private static ChatMessage CreateMessage() => new(
        "Hot lead",
        "Jane is ready to buy.",
        [("Process", "Lead nurture"), ("Email", "jane@example.test")],
        [new ChatLink(ChatMessage.LINK_CONTACT, link)]);


    private static List<ChatLink> CreateThreeLinks() =>
        [new ChatLink(ChatMessage.LINK_CONTACT, link), new ChatLink(ChatMessage.LINK_FORM_SUBMISSION, formLink), new ChatLink(ChatMessage.LINK_PROCESS_CONTACTS, processLink)];
}
