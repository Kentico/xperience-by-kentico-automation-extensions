using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Localization;

/// <summary>
/// Missing keys show raw resource keys to marketers, so every key used by the library must exist in the English resources.
/// </summary>
public partial class LocalizationResourceTests
{
    private const string KEY_PREFIX = "labs.automationextensions.";


    private static readonly ResourceManager resourceManager = new(typeof(AutomationExtensionsResources));


    [Test]
    public void RegistrationAttributes_DisplayNameAndDescriptionKeysExist()
    {
        var keys = AutomationComponentCatalogTests.GetRegistrationAttributes()
            .SelectMany(attribute => new[]
            {
                (string?)attribute.GetType().GetProperty("DisplayName")!.GetValue(attribute),
                (string?)attribute.GetType().GetProperty("Description")!.GetValue(attribute)
            })
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(keys, Is.All.StartsWith(KEY_PREFIX));
            AssertKeysExist(keys!);
        });
    }


    [Test]
    public void PropertyAttributes_MacroKeysExist()
    {
        var keys = typeof(AutomationComponentCatalog).Assembly
            .GetTypes()
            // Declared properties only: library types also derive from platform types (e.g. visibility condition properties)
            // whose inherited properties use platform resource keys. Library base types are scanned on their own.
            .SelectMany(type => type.GetCustomAttributes().Concat(type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(property => property.GetCustomAttributes())))
            .SelectMany(attribute => attribute.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
                .Select(property => property.GetValue(attribute) as string))
            .Where(value => value is not null)
            .SelectMany(value => MacroPattern().Matches(value!).Select(match => match.Groups["key"].Value))
            .Distinct()
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(keys, Is.Not.Empty);
            AssertKeysExist(keys);
        });
    }


    [Test]
    public void ComponentNames_HaveDisplayNameKeys()
    {
        var keys = AutomationComponentCatalog.All
            .Select(component => $"{KEY_PREFIX}{component.Name.ToLowerInvariant()}.displayname")
            .ToList();

        AssertKeysExist(keys);
    }


    [Test]
    public void LibraryStepResultSources_DisplayNameKeysExist() => AssertKeysExist([StepResultKeys.SendWebhook.DisplayName]);


    [TestCase("labs.automationextensions.randomsplit.description")]
    [TestCase("labs.automationextensions.randomsplit.percentage.explanation")]
    public void RandomSplit_BuilderHelpExplainsDisabledBranchAndSkipPrecedence(string key)
    {
        string? text = resourceManager.GetString(key, CultureInfo.InvariantCulture);

        Assert.That(text, Does.Contain("all contacts follow No (B)").And.Contain("regardless of").And.Contain("skip settings"));
    }


    [TestCase("labs.automationextensions.skipstep.condition.explanation", "follow No,")]
    [TestCase("labs.automationextensions.stepregistry.disabled.content", "\"No\" path")]
    public void DisabledConditionHelp_ExplainsNoBranchForAllLibraryConditions(string key, string branchText)
    {
        string? text = resourceManager.GetString(key, CultureInfo.InvariantCulture);

        Assert.That(text, Does.Contain(branchText).And.Contain("disabled"));
    }


    [TestCase("labs.automationextensions.stepresult.number.text")]
    [TestCase("labs.automationextensions.contactdateanniversary.daysbefore.ontheday")]
    [TestCase("labs.automationextensions.contactdateanniversary.daysbefore.format")]
    [TestCase("labs.automationextensions.adjustcontactscore.operation.add")]
    [TestCase("labs.automationextensions.adjustcontactscore.operation.subtract")]
    [TestCase("labs.automationextensions.adjustcontactscore.operation.set")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.equal")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.notequal")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.greaterthan")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.greaterthanorequal")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.lessthan")]
    [TestCase("labs.automationextensions.contactnumberfieldcompare.operator.lessthanorequal")]
    public void CodeKeys_Exist(string key) => AssertKeysExist([key]);


    private static void AssertKeysExist(IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            Assert.That(resourceManager.GetString(key, CultureInfo.InvariantCulture), Is.Not.Null.And.Not.Empty, $"Missing resource key '{key}'.");
        }
    }


    [GeneratedRegex(@"\{\$(?<key>[^$]+)\$\}")]
    private static partial Regex MacroPattern();
}
