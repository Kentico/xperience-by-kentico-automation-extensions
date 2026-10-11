using CMS.Core;
using CMS.FormEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactScoreFieldResolverTests
{
    /// <summary>
    /// Shape of the stock contact class definition (built-in fields are marked <c>system="true"</c>, built-in ID references
    /// carry no reference metadata) plus custom fields.
    /// </summary>
    internal const string CONTACT_FORM_DEFINITION = """
        <form>
          <field column="ContactID" columntype="integer" isPK="true" isunique="true" system="true" />
          <field column="ContactFirstName" columntype="text" columnsize="100" allowempty="true" system="true" />
          <field column="ContactGender" columntype="integer" allowempty="true" system="true" />
          <field column="ContactStatusID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactOwnerUserID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactStateID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactCountryID" columntype="integer" allowempty="true" system="true" />
          <field column="ContactBounces" columntype="integer" allowempty="true" system="true" />
          <field column="AcmeContactScore" columntype="integer" allowempty="true" fieldcaption="Lead score" />
          <field column="AcmeContactEngagement" columntype="integer" allowempty="true" />
          <field column="AcmeContactAccountManagerID" columntype="integer" allowempty="true" refobjtype="cms.user" reftype="NotRequired" />
          <field column="AcmeContactRevenue" columntype="longinteger" allowempty="true" />
          <field column="AcmeContactRating" columntype="decimal" allowempty="true" />
          <field column="AcmeContactWeight" columntype="double" allowempty="true" />
          <field column="AcmeContactNickname" columntype="text" columnsize="50" allowempty="true" />
          <field column="AcmeContactDummy" columntype="integer" allowempty="true" dummy="mainform" />
        </form>
        """;


    [Test]
    public void GetFields_ReturnsOnlyCustomIntegerFieldsSortedByCaption()
    {
        var resolver = CreateResolver();

        var fields = resolver.GetFields();

        Assert.That(fields, Is.EqualTo(new[]
        {
            new ContactScoreField("AcmeContactEngagement", "AcmeContactEngagement"),
            new ContactScoreField("AcmeContactScore", "Lead score")
        }));
    }


    [Test]
    public void GetFields_LocalizesCaption()
    {
        var resolver = new ContactScoreFieldResolver(new TestLocalization(text => text.ToUpperInvariant()), () => new FormInfo(CONTACT_FORM_DEFINITION));

        Assert.That(resolver.GetFields().Select(field => field.Caption), Does.Contain("LEAD SCORE"));
    }


    [Test]
    public void GetFields_MissingDefinitionReturnsEmpty() => Assert.That(
        new ContactScoreFieldResolver(new TestLocalization(text => text), () => null).GetFields(),
        Is.Empty);


    [TestCase("AcmeContactScore", ExpectedResult = "AcmeContactScore")]
    [TestCase("acmecontactscore", ExpectedResult = "AcmeContactScore")]
    [TestCase("ContactID", ExpectedResult = null)]
    [TestCase("ContactOwnerUserID", ExpectedResult = null)]
    [TestCase("ContactStatusID", ExpectedResult = null)]
    [TestCase("ContactCountryID", ExpectedResult = null)]
    [TestCase("ContactStateID", ExpectedResult = null)]
    [TestCase("ContactBounces", ExpectedResult = null)]
    [TestCase("AcmeContactAccountManagerID", ExpectedResult = null)]
    [TestCase("AcmeContactRevenue", ExpectedResult = null)]
    [TestCase("AcmeContactRating", ExpectedResult = null)]
    [TestCase("AcmeContactWeight", ExpectedResult = null)]
    [TestCase("AcmeContactNickname", ExpectedResult = null)]
    [TestCase("AcmeContactDummy", ExpectedResult = null)]
    [TestCase("AcmeContactScore; DROP TABLE OM_Contact", ExpectedResult = null)]
    [TestCase("Missing", ExpectedResult = null)]
    [TestCase("", ExpectedResult = null)]
    [TestCase(null, ExpectedResult = null)]
    public string? ResolveFieldName_ReturnsDefinedNameOfEligibleFieldsOnly(string? fieldName) =>
        CreateResolver().ResolveFieldName(fieldName);


    [Test]
    public void ResolveFieldName_BlankNameDoesNotReadDefinition()
    {
        var resolver = new ContactScoreFieldResolver(
            new TestLocalization(text => text),
            () => throw new InvalidOperationException("Definition must not be read."));

        Assert.That(resolver.ResolveFieldName(" "), Is.Null);
    }


    internal static ContactScoreFieldResolver CreateResolver() =>
        new(new TestLocalization(text => text), () => new FormInfo(CONTACT_FORM_DEFINITION));


    internal sealed class TestLocalization(Func<string, string> localize) : ILocalizationService
    {
        public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true) => resourceKey;


        public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) =>
            localize(inputText);


        public string LocalizeExpression(string expression, string? culture = null, bool encode = false,
            Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) => expression;
    }
}
