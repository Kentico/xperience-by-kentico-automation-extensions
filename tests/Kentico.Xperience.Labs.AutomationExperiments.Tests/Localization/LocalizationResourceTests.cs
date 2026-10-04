using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Core;
using Kentico.Xperience.Labs.AutomationExperiments.Resources;
using Kentico.Xperience.Labs.AutomationExperiments.Tests.Configuration;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Localization;

/// <summary>
/// Missing keys show raw resource keys to marketers, so every key used by the library must exist in the English resources.
/// </summary>
public partial class LocalizationResourceTests
{
    private const string KEY_PREFIX = "labs.automationexperiments.";


    private static readonly ResourceManager resourceManager = new(typeof(AutomationExperimentsResources));


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
            .SelectMany(type => type.GetCustomAttributes().Concat(type.GetProperties().SelectMany(property => property.GetCustomAttributes())))
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
    public void LibraryStepResultSources_DisplayNameKeysExist()
    {
        AssertKeysExist([StepResultKeys.SendWebhook.DisplayName]);
    }


    [TestCase("labs.automationexperiments.stepresult.number.text")]
    [TestCase("labs.automationexperiments.contactdateanniversary.daysbefore.ontheday")]
    [TestCase("labs.automationexperiments.contactdateanniversary.daysbefore.format")]
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
