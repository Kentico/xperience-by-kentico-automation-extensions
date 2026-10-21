using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ChatMessageFactoryTests
{
    private static readonly Uri link = new("https://example.test/admin/contact-management/contacts/42/overview");
    private static readonly Uri formLink = new("https://example.test/admin/forms/list/2/submissions/15");
    private static readonly Uri processLink = new("https://example.test/admin/automation/list/4002/automation-contacts");


    [Test]
    public void Create_ResolvesTokensInTitleAndMessage()
    {
        var properties = new PostChatMessageActionProperties
        {
            Title = "Hot lead {contact.ContactFirstName}",
            Message = "{processName}: contact {contactId} on {today}\nScore {contact.AcmeCounter}"
        };
        var unresolved = new List<string>();

        var message = ChatMessageFactory.Create(properties, CreateTokens(), null, null, null, null, unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(message.Title, Is.EqualTo("Hot lead Jane"));
            Assert.That(message.Text, Is.EqualTo("Lead nurture: contact 42 on 2026-10-21\nScore 7"));
            Assert.That(unresolved, Is.Empty);
        });
    }


    [Test]
    public void Create_UnresolvedTokensBecomeEmptyAndAreReported()
    {
        var properties = new PostChatMessageActionProperties { Title = "{unknown}", Message = "a{contact.Missing}b" };
        var unresolved = new List<string>();

        var message = ChatMessageFactory.Create(properties, CreateTokens(), null, null, null, null, unresolved);

        Assert.Multiple(() =>
        {
            Assert.That(message.Title, Is.Empty);
            Assert.That(message.Text, Is.EqualTo("ab"));
            Assert.That(unresolved, Is.EqualTo(new[] { "unknown", "contact.Missing" }));
        });
    }


    [Test]
    public void Create_WithoutContactDetails_OnlyProcessFact()
    {
        var message = ChatMessageFactory.Create(new PostChatMessageActionProperties { Message = "x" }, CreateTokens(), null, null, null, null, []);

        Assert.That(message.Facts, Is.EqualTo(new[] { ("Process", "Lead nurture") }));
    }


    [Test]
    public void Create_WithContactDetails_AddsNonEmptyDetails()
    {
        var details = new ChatContactDetails("jane@example.test", " Jane ", null, "");

        var message = ChatMessageFactory.Create(new PostChatMessageActionProperties { Message = "x" }, CreateTokens(), details, null, null, null, []);

        Assert.That(message.Facts, Is.EqualTo(new[] { ("Process", "Lead nurture"), ("Email", "jane@example.test"), ("Name", "Jane") }));
    }


    [Test]
    public void Create_FullContactDetails_JoinsName()
    {
        var details = new ChatContactDetails("jane@example.test", "Jane", "Doe", "Acme");

        var message = ChatMessageFactory.Create(new PostChatMessageActionProperties { Message = "x" }, CreateTokens(), details, null, null, null, []);

        Assert.That(message.Facts, Is.EqualTo(new[] { ("Process", "Lead nurture"), ("Email", "jane@example.test"), ("Name", "Jane Doe"), ("Company", "Acme") }));
    }


    [TestCase(true, true, true, new[] { "Open contact", "Open form submission", "Open process contacts" })]
    [TestCase(true, true, false, new[] { "Open contact", "Open form submission" })]
    [TestCase(true, false, true, new[] { "Open contact", "Open process contacts" })]
    [TestCase(false, true, true, new[] { "Open form submission", "Open process contacts" })]
    [TestCase(true, false, false, new[] { "Open contact" })]
    [TestCase(false, true, false, new[] { "Open form submission" })]
    [TestCase(false, false, true, new[] { "Open process contacts" })]
    [TestCase(false, false, false, new string[0])]
    public void Create_LinkProperties_ControlLinks(bool includeAdminLink, bool includeFormSubmissionLink, bool includeProcessLink, string[] expectedLabels)
    {
        var properties = new PostChatMessageActionProperties
        {
            Message = "x",
            IncludeAdminLink = includeAdminLink,
            IncludeFormSubmissionLink = includeFormSubmissionLink,
            IncludeProcessLink = includeProcessLink
        };

        var message = ChatMessageFactory.Create(properties, CreateTokens(), null, link, formLink, processLink, []);

        Assert.That(message.LinkList.Select(chatLink => chatLink.Label), Is.EqualTo(expectedLabels));
    }


    [Test]
    public void Create_MissingLinks_Skipped()
    {
        var message = ChatMessageFactory.Create(new PostChatMessageActionProperties { Message = "x" }, CreateTokens(), null, null, formLink, null, []);

        Assert.That(message.LinkList, Is.EqualTo(new[] { new ChatLink(ChatMessage.LINK_FORM_SUBMISSION, formLink) }));
    }


    [Test]
    public void GetFormSubmissionLink_FormSubmissionTrigger_ReturnsSubmissionUrl()
    {
        var data = new FormSubmissionTriggerData { BizFormId = 2, BizFormItemId = 15 };

        var result = ChatMessageFactory.GetFormSubmissionLink("http://localhost:36259/admin", DefaultTriggerIdentifiers.FORM_SUBMISSION, data);

        Assert.That(result?.AbsoluteUri, Is.EqualTo("http://localhost:36259/admin/forms/list/2/submissions/15"));
    }


    [Test]
    public void GetFormSubmissionLink_OtherTrigger_ReturnsNull() =>
        Assert.That(ChatMessageFactory.GetFormSubmissionLink(
            "https://example.test/admin", DefaultTriggerIdentifiers.REGISTRATION, new FormSubmissionTriggerData { BizFormId = 2, BizFormItemId = 15 }), Is.Null);


    [Test]
    public void GetFormSubmissionLink_MissingOrOtherTriggerData_ReturnsNull() => Assert.Multiple(() =>
    {
        Assert.That(ChatMessageFactory.GetFormSubmissionLink("https://example.test/admin", DefaultTriggerIdentifiers.FORM_SUBMISSION, null), Is.Null);
        Assert.That(ChatMessageFactory.GetFormSubmissionLink("https://example.test/admin", DefaultTriggerIdentifiers.FORM_SUBMISSION, new RegistrationTriggerData()), Is.Null);
    });


    [Test]
    public void GetFormSubmissionLink_NoAdminBaseUrl_ReturnsNull() =>
        Assert.That(ChatMessageFactory.GetFormSubmissionLink(
            "", DefaultTriggerIdentifiers.FORM_SUBMISSION, new FormSubmissionTriggerData { BizFormId = 2, BizFormItemId = 15 }), Is.Null);


    [Test]
    public void Properties_LinksOnByDefault() => Assert.Multiple(() =>
    {
        Assert.That(new PostChatMessageActionProperties().IncludeAdminLink, Is.True);
        Assert.That(new PostChatMessageActionProperties().IncludeFormSubmissionLink, Is.True);
        Assert.That(new PostChatMessageActionProperties().IncludeProcessLink, Is.True);
    });


    [Test]
    public async Task GetProcessLink_SingleMatch_ReturnsContactsTabUrl()
    {
        var resolver = new StubProcessResolver(4002);

        var result = await ChatMessageFactory.GetProcessLink(new PostChatMessageActionProperties(), "http://localhost:36259/admin", "Lead nurture", resolver, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result?.AbsoluteUri, Is.EqualTo("http://localhost:36259/admin/automation/list/4002/automation-contacts"));
            Assert.That(resolver.Names, Is.EqualTo(new[] { "Lead nurture" }));
        });
    }


    [Test]
    public async Task GetProcessLink_NoUniqueMatch_ReturnsNull() =>
        Assert.That(await ChatMessageFactory.GetProcessLink(new PostChatMessageActionProperties(), "https://example.test/admin", "Lead nurture", new StubProcessResolver(null), CancellationToken.None), Is.Null);


    [TestCase(false, "https://example.test/admin")]
    [TestCase(true, "")]
    [TestCase(true, "not a url")]
    public async Task GetProcessLink_PropertyOffOrNoAdminBaseUrl_DoesNotLookUpProcess(bool includeProcessLink, string adminBaseUrl)
    {
        var resolver = new StubProcessResolver(4002);
        var properties = new PostChatMessageActionProperties { IncludeProcessLink = includeProcessLink };

        var result = await ChatMessageFactory.GetProcessLink(properties, adminBaseUrl, "Lead nurture", resolver, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(resolver.Names, Is.Empty);
        });
    }


    private sealed class StubProcessResolver(int? processId) : IAutomationProcessResolver
    {
        public List<string?> Names { get; } = [];


        public Task<int?> FindIdByDisplayName(string? displayName, CancellationToken cancellationToken)
        {
            Names.Add(displayName);
            return Task.FromResult(processId);
        }
    }


    private static ContactFieldTokenValues CreateTokens() => new()
    {
        Now = new DateTime(2026, 10, 21, 9, 30, 0, DateTimeKind.Local),
        ProcessName = "Lead nurture",
        ContactId = 42,
        GetContactFieldValue = name => name switch
        {
            "ContactFirstName" => "Jane",
            "AcmeCounter" => "7",
            _ => null
        }
    };
}
