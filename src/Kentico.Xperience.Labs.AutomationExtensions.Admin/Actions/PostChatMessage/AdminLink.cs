using System.Globalization;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;

/// <summary>
/// Builds URLs of pages in the Xperience administration.
/// </summary>
/// <remarks>
/// Routes verified against the UI page registrations of Kentico.Xperience.Admin.DigitalMarketing 31.9.3. Recheck after
/// Xperience updates.
/// </remarks>
internal static class AdminLink
{
    /// <summary>
    /// Route of the contact overview page below the admin root; <c>{0}</c> = contact ID.
    /// </summary>
    /// <remarks>
    /// <c>ContactManagementApplication</c> (slug <c>contact-management</c>) → <c>ContactList</c> (<c>contacts</c>) →
    /// <c>ContactEditSection</c> (<c>EditSectionPage&lt;ContactInfo&gt;</c>, page parameter <c>ObjectId</c> = contact ID) →
    /// <c>ContactOverview</c> (<c>overview</c>).
    /// </remarks>
    public const string CONTACT_OVERVIEW_ROUTE = "contact-management/contacts/{0}/overview";


    /// <summary>
    /// Route of the form submission detail page below the admin root; <c>{0}</c> = form ID, <c>{1}</c> = submission (form item) ID.
    /// </summary>
    /// <remarks>
    /// <c>FormsApplication</c> (slug <c>forms</c>) → <c>FormList</c> (<c>list</c>) → <c>FormEditSection</c>
    /// (<c>EditSectionPage&lt;BizFormInfo&gt;</c>, <c>ObjectId</c> = form ID) → <c>FormSubmissionsTab</c> (<c>submissions</c>) →
    /// <c>FormSubmissionDetails</c> (parameter slug, <c>ObjectId</c> = submission ID).
    /// </remarks>
    public const string FORM_SUBMISSION_ROUTE = "forms/list/{0}/submissions/{1}";


    /// <summary>
    /// Route of the automation process Contacts tab below the admin root; <c>{0}</c> = process (<c>WorkflowInfo</c>) ID.
    /// </summary>
    /// <remarks>
    /// <c>AutomationApplication</c> (slug <c>automation</c>) → <c>AutomationProcessList</c> (<c>list</c>) →
    /// <c>AutomationProcessEditSection</c> (<c>EditSectionPage&lt;WorkflowInfo&gt;</c>, <c>ObjectId</c> = process ID) →
    /// <c>AutomationProcessContactsTab</c> (<c>automation-contacts</c>).
    /// </remarks>
    public const string PROCESS_CONTACTS_ROUTE = "automation/list/{0}/automation-contacts";


    private const char PATH_SEPARATOR = '/';


    /// <summary>
    /// Returns the contact overview URL, or <c>null</c> when <paramref name="adminBaseUrl"/> is empty or not an absolute
    /// HTTP(S) URL.
    /// </summary>
    /// <param name="adminBaseUrl">Absolute URL of the admin root, with or without trailing slash (e.g. <c>https://www.example.com/admin</c>).</param>
    /// <param name="contactId">Contact ID.</param>
    public static Uri? ContactOverview(string? adminBaseUrl, int contactId) =>
        Create(adminBaseUrl, string.Format(CultureInfo.InvariantCulture, CONTACT_OVERVIEW_ROUTE, contactId));


    /// <summary>
    /// Returns the form submission URL, or <c>null</c> when <paramref name="adminBaseUrl"/> is empty or invalid, or an ID
    /// isn't positive.
    /// </summary>
    /// <param name="adminBaseUrl">Absolute URL of the admin root, with or without trailing slash.</param>
    /// <param name="formId">Form ID (<c>BizFormInfo.FormID</c>).</param>
    /// <param name="submissionId">Submission (form item) ID.</param>
    public static Uri? FormSubmission(string? adminBaseUrl, int formId, int submissionId) =>
        formId <= 0 || submissionId <= 0
            ? null
            : Create(adminBaseUrl, string.Format(CultureInfo.InvariantCulture, FORM_SUBMISSION_ROUTE, formId, submissionId));


    /// <summary>
    /// Returns the URL of the automation process Contacts tab, or <c>null</c> when <paramref name="adminBaseUrl"/> is empty
    /// or invalid, or the ID isn't positive.
    /// </summary>
    /// <param name="adminBaseUrl">Absolute URL of the admin root, with or without trailing slash.</param>
    /// <param name="processId">Automation process (<c>WorkflowInfo</c>) ID.</param>
    public static Uri? ProcessContacts(string? adminBaseUrl, int processId) =>
        processId <= 0
            ? null
            : Create(adminBaseUrl, string.Format(CultureInfo.InvariantCulture, PROCESS_CONTACTS_ROUTE, processId));


    /// <summary>
    /// Returns <c>true</c> when <paramref name="adminBaseUrl"/> is an absolute HTTP(S) URL that links can be built from.
    /// </summary>
    public static bool IsValidBaseUrl(string? adminBaseUrl) => Create(adminBaseUrl, string.Empty) is not null;


    private static Uri? Create(string? adminBaseUrl, string route)
    {
        if (string.IsNullOrWhiteSpace(adminBaseUrl)
            || !Uri.TryCreate(adminBaseUrl.Trim().TrimEnd(PATH_SEPARATOR) + PATH_SEPARATOR, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        return new Uri(baseUri, route);
    }
}
