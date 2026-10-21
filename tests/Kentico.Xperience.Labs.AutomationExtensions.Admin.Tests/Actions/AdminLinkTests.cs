using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class AdminLinkTests
{
    [TestCase("https://www.example.test/admin")]
    [TestCase("https://www.example.test/admin/")]
    [TestCase(" https://www.example.test/admin// ")]
    public void ContactOverview_BaseUrlWithOrWithoutTrailingSlash_ReturnsOverviewUrl(string adminBaseUrl) =>
        Assert.That(AdminLink.ContactOverview(adminBaseUrl, 42)?.AbsoluteUri,
            Is.EqualTo("https://www.example.test/admin/contact-management/contacts/42/overview"));


    [Test]
    public void ContactOverview_LocalAdmin_ReturnsOverviewUrl() =>
        Assert.That(AdminLink.ContactOverview("http://localhost:36259/admin", 7)?.AbsoluteUri,
            Is.EqualTo("http://localhost:36259/admin/contact-management/contacts/7/overview"));


    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("admin")]
    [TestCase("/admin")]
    [TestCase("ftp://example.test/admin")]
    public void ContactOverview_MissingOrInvalidBaseUrl_ReturnsNull(string? adminBaseUrl) =>
        Assert.That(AdminLink.ContactOverview(adminBaseUrl, 42), Is.Null);


    [TestCase("http://localhost:36259/admin")]
    [TestCase("http://localhost:36259/admin/")]
    public void FormSubmission_ReturnsSubmissionUrl(string adminBaseUrl) =>
        Assert.That(AdminLink.FormSubmission(adminBaseUrl, 2, 15)?.AbsoluteUri,
            Is.EqualTo("http://localhost:36259/admin/forms/list/2/submissions/15"));


    [TestCase("", 2, 15)]
    [TestCase("not a url", 2, 15)]
    [TestCase("https://example.test/admin", 0, 15)]
    [TestCase("https://example.test/admin", 2, 0)]
    public void FormSubmission_MissingBaseUrlOrInvalidIds_ReturnsNull(string adminBaseUrl, int formId, int submissionId) =>
        Assert.That(AdminLink.FormSubmission(adminBaseUrl, formId, submissionId), Is.Null);


    [TestCase("http://localhost:36259/admin")]
    [TestCase("http://localhost:36259/admin/")]
    public void ProcessContacts_ReturnsContactsTabUrl(string adminBaseUrl) =>
        Assert.That(AdminLink.ProcessContacts(adminBaseUrl, 4002)?.AbsoluteUri,
            Is.EqualTo("http://localhost:36259/admin/automation/list/4002/automation-contacts"));


    [TestCase("", 4002)]
    [TestCase("not a url", 4002)]
    [TestCase("https://example.test/admin", 0)]
    public void ProcessContacts_MissingBaseUrlOrInvalidId_ReturnsNull(string adminBaseUrl, int processId) =>
        Assert.That(AdminLink.ProcessContacts(adminBaseUrl, processId), Is.Null);


    [TestCase("https://example.test/admin", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("ftp://example.test/admin", false)]
    public void IsValidBaseUrl(string? adminBaseUrl, bool expected) => Assert.That(AdminLink.IsValidBaseUrl(adminBaseUrl), Is.EqualTo(expected));
}
