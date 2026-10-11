using CMS.DataEngine;
using CMS.FormEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactFieldResolverTests
{
    /// <summary>
    /// Stock contact class definition of Xperience 31.9 (built-in fields marked <c>system="true"</c>, no captions) plus
    /// custom fields of all data types.
    /// </summary>
    internal const string CONTACT_FORM_DEFINITION = """
        <form>
          <field column="ContactID" columntype="integer" isPK="true" isunique="true" system="true" />
          <field column="ContactFirstName" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactMiddleName" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactLastName" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactBirthday" columntype="datetime" allowempty="true" system="true" />
          <field column="ContactGender" columntype="integer" allowempty="true" system="true" />
          <field column="ContactCompanyName" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactJobTitle" columntype="text" columnsize="50" allowempty="true" system="true" />
          <field column="ContactCreated" columntype="datetime" system="true" />
          <field column="ContactStatusID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactMonitored" columntype="boolean" allowempty="true" system="true" />
          <field column="ContactOwnerUserID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactCampaign" columntype="text" columnsize="200" allowempty="true" system="true" />
          <field column="ContactAddress1" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactCity" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactZIP" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactStateID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactCountryID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactMobilePhone" columntype="text" columnsize="26" allowempty="true" system="true" />
          <field column="ContactBusinessPhone" columntype="text" columnsize="26" allowempty="true" system="true" />
          <field column="ContactEmail" columntype="text" columnsize="254" allowempty="true" system="true" />
          <field column="ContactNotes" columntype="longtext" allowempty="true" system="true" />
          <field column="ContactGUID" columntype="guid" system="true" />
          <field column="ContactLastModified" columntype="datetime" system="true" />
          <field column="ContactBounces" columntype="integer" allowempty="true" system="true" />
          <field column="ContactSalesForceLeadID" columntype="text" columnsize="18" allowempty="true" system="true" />
          <field column="ContactSalesForceLeadReplicationDisabled" columntype="boolean" allowempty="true" system="true" />
          <field column="ContactSalesForceLeadReplicationDateTime" columntype="datetime" allowempty="true" system="true" />
          <field column="ContactSalesForceLeadReplicationSuspensionDateTime" columntype="datetime" allowempty="true" system="true" />
          <field column="ContactSalesForceLeadReplicationRequired" columntype="boolean" allowempty="true" system="true" />
          <field column="AcmeLastCampaign" columntype="text" columnsize="10" allowempty="true" fieldcaption="Last campaign" />
          <field column="AcmeRequiredCode" columntype="text" columnsize="20" />
          <field column="AcmeNotes" columntype="longtext" allowempty="true" />
          <field column="AcmeCounter" columntype="integer" allowempty="true" fieldcaption="Counter" />
          <field column="AcmeRevenue" columntype="longinteger" allowempty="true" />
          <field column="AcmeRating" columntype="decimal" columnsize="10" columnprecision="2" allowempty="true" />
          <field column="AcmeWeight" columntype="double" allowempty="true" />
          <field column="AcmeVip" columntype="boolean" allowempty="true" />
          <field column="AcmeRequiredFlag" columntype="boolean" />
          <field column="AcmeFirstPurchase" columntype="date" allowempty="true" />
          <field column="AcmeLastVisit" columntype="datetime" allowempty="true" />
          <field column="AcmeExternalId" columntype="guid" allowempty="true" />
          <field column="AcmeAvatar" columntype="binary" allowempty="true" />
          <field column="AcmeDuration" columntype="timespan" allowempty="true" />
          <field column="AcmeDummy" columntype="text" allowempty="true" dummy="mainform" />
        </form>
        """;


    [TestCase("ContactID", Description = "Primary key")]
    [TestCase("ContactGUID", Description = "Identity, GUID type")]
    [TestCase("ContactCreated", Description = "System timestamp")]
    [TestCase("ContactLastModified", Description = "System timestamp")]
    [TestCase("ContactBounces", Description = "Maintained by email bounce handling")]
    [TestCase("ContactGender", Description = "Enum")]
    [TestCase("ContactStatusID", Description = "Foreign key")]
    [TestCase("ContactOwnerUserID", Description = "Foreign key")]
    [TestCase("ContactCountryID", Description = "Foreign key")]
    [TestCase("ContactStateID", Description = "Foreign key")]
    [TestCase("ContactSalesForceLeadID")]
    [TestCase("ContactSalesForceLeadReplicationDisabled")]
    [TestCase("ContactSalesForceLeadReplicationDateTime")]
    [TestCase("ContactSalesForceLeadReplicationSuspensionDateTime")]
    [TestCase("ContactSalesForceLeadReplicationRequired")]
    [TestCase("AcmeExternalId", Description = "GUID type")]
    [TestCase("AcmeAvatar", Description = "Binary type")]
    [TestCase("AcmeDuration", Description = "Time span type")]
    [TestCase("AcmeDummy", Description = "Not a database field")]
    [TestCase("Missing")]
    [TestCase("AcmeCounter; DROP TABLE OM_Contact")]
    [TestCase("")]
    [TestCase(null)]
    public void Resolve_ExcludedFieldsReturnNull(string? fieldName) => Assert.Multiple(() =>
    {
        Assert.That(CreateResolver().Resolve(fieldName), Is.Null);
        Assert.That(CreateResolver().GetFields().Select(field => field.Name), Does.Not.Contain(fieldName));
    });


    [Test]
    public void IsWritable_ReferenceFieldIsExcluded()
    {
        var field = new FormFieldInfo { Name = "AcmeAccountManagerID", DataType = FieldDataType.Integer, AllowEmpty = true };
        Assert.That(ContactFieldResolver.IsWritable(field), Is.True);

        field.ReferenceToObjectType = "cms.user";
        Assert.That(ContactFieldResolver.IsWritable(field), Is.False);
    }


    [TestCase("ContactFirstName", "text")]
    [TestCase("ContactEmail", "text")]
    [TestCase("ContactNotes", "longtext")]
    [TestCase("ContactBirthday", "datetime")]
    [TestCase("ContactMonitored", "boolean")]
    [TestCase("ContactCampaign", "text")]
    [TestCase("AcmeLastCampaign", "text")]
    [TestCase("AcmeNotes", "longtext")]
    [TestCase("AcmeCounter", "integer")]
    [TestCase("AcmeRevenue", "longinteger")]
    [TestCase("AcmeRating", "decimal")]
    [TestCase("AcmeWeight", "double")]
    [TestCase("AcmeVip", "boolean")]
    [TestCase("AcmeFirstPurchase", "date")]
    [TestCase("AcmeLastVisit", "datetime")]
    public void Resolve_SupportedFieldsReturnDataType(string fieldName, string dataType)
    {
        var field = CreateResolver().Resolve(fieldName.ToUpperInvariant());

        Assert.Multiple(() =>
        {
            Assert.That(field?.Name, Is.EqualTo(fieldName));
            Assert.That(field?.DataType, Is.EqualTo(dataType));
        });
    }


    [Test]
    public void Resolve_ReturnsSizeAllowEmptyAndSystemFlag()
    {
        var resolver = CreateResolver();

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Resolve("AcmeLastCampaign"), Is.EqualTo(new ContactField("AcmeLastCampaign", "Last campaign", "text", 10, true, false)));
            Assert.That(resolver.Resolve("AcmeRequiredCode")!.AllowEmpty, Is.False);
            Assert.That(resolver.Resolve("ContactEmail")!.IsSystem, Is.True);
        });
    }


    [Test]
    public void GetFields_CustomFieldsFirstThenSystemFieldsSortedByCaption()
    {
        var fields = CreateResolver().GetFields();
        var custom = fields.TakeWhile(field => !field.IsSystem).Select(field => field.Caption).ToList();
        var system = fields.SkipWhile(field => !field.IsSystem).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(custom, Has.Count.EqualTo(11));
            Assert.That(custom, Is.Ordered.Using((IComparer<string>)StringComparer.CurrentCultureIgnoreCase));
            Assert.That(system, Has.All.Matches<ContactField>(field => field.IsSystem));
            Assert.That(system.Select(field => field.Caption), Is.Ordered.Using((IComparer<string>)StringComparer.CurrentCultureIgnoreCase));
            Assert.That(system, Has.Count.EqualTo(15));
        });
    }


    [Test]
    public void GetFields_LocalizesCaption() => Assert.That(
        new ContactFieldResolver(new ContactScoreFieldResolverTests.TestLocalization(text => text.ToUpperInvariant()), () => new FormInfo(CONTACT_FORM_DEFINITION))
            .GetFields().Select(field => field.Caption),
        Does.Contain("LAST CAMPAIGN"));


    [Test]
    public void GetFields_MissingDefinitionReturnsEmpty() => Assert.That(
        new ContactFieldResolver(new ContactScoreFieldResolverTests.TestLocalization(text => text), () => null).GetFields(),
        Is.Empty);


    [TestCase("ContactGUID")]
    [TestCase("ContactCreated")]
    [TestCase("contactid")]
    [TestCase("AcmeExternalId")]
    [TestCase("AcmeCounter")]
    public void ResolveReadable_IncludesSystemMaintainedFields(string fieldName) =>
        Assert.That(CreateResolver().ResolveReadable(fieldName), Is.Not.Null);


    [TestCase("AcmeDummy")]
    [TestCase("Missing")]
    [TestCase("")]
    public void ResolveReadable_ExcludesNonDatabaseAndUnknownFields(string fieldName) =>
        Assert.That(CreateResolver().ResolveReadable(fieldName), Is.Null);


    [Test]
    public void Resolve_BlankNameDoesNotReadDefinition()
    {
        var resolver = new ContactFieldResolver(
            new ContactScoreFieldResolverTests.TestLocalization(text => text),
            () => throw new InvalidOperationException("Definition must not be read."));

        Assert.That(resolver.Resolve(" "), Is.Null);
    }


    [Test]
    public void OptionText_ShowsCaptionAndCodeNameAndMarksSystemFields() => Assert.Multiple(() =>
    {
        Assert.That(ContactFieldOptionsProvider.GetText(new ContactField("AcmeCounter", "Counter", "integer", 0, true, false), "{0} – system field"),
            Is.EqualTo("Counter (AcmeCounter)"));
        Assert.That(ContactFieldOptionsProvider.GetText(new ContactField("AcmeNotes", "AcmeNotes", "longtext", 0, true, false), "{0} – system field"),
            Is.EqualTo("AcmeNotes"));
        Assert.That(ContactFieldOptionsProvider.GetText(new ContactField("ContactEmail", "ContactEmail", "text", 254, true, true), "{0} – system field"),
            Is.EqualTo("ContactEmail – system field"));
    });


    [Test]
    public async Task OptionsProvider_ListsChangeableFields()
    {
        var provider = new ContactFieldOptionsProvider(CreateResolver(), new ContactScoreFieldResolverTests.TestLocalization(text => text));

        var items = (await provider.GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(CreateResolver().GetFields().Select(field => field.Name)));
            Assert.That(items[0].Text, Is.EqualTo("AcmeFirstPurchase"));
        });
    }


    internal static ContactFieldResolver CreateResolver() =>
        new(new ContactScoreFieldResolverTests.TestLocalization(text => text), () => new FormInfo(CONTACT_FORM_DEFINITION));


    internal static ContactField GetField(string name) => CreateResolver().ResolveReadable(name)!;
}
