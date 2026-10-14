using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

using CMS;
using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.Configuration;

/// <summary>
/// Assembly-level checks of the commerce assembly (mirrors the Admin tests: discovery, catalog, localization, logging).
/// </summary>
public partial class CommerceAssemblyTests
{
    private const int EVENT_LOG_COLUMN_LENGTH = 100;
    private const string KEY_PREFIX = "labs.automationextensions.";

    private static readonly Assembly assembly = typeof(CommerceAutomationComponents).Assembly;
    private static readonly ResourceManager resourceManager = new(typeof(AutomationExtensionsCommerceResources));


    [Test]
    public void Assembly_IsDiscoverable() => Assert.That(assembly.GetCustomAttribute<AssemblyDiscoverableAttribute>(), Is.Not.Null);


    [Test]
    public void All_MatchesRegisteredComponents() =>
        Assert.That(CommerceAutomationComponents.All.Select(component => component.Identifier), Is.EquivalentTo(GetRegisteredIdentifiers()));


    [Test]
    public void All_NamesDoNotClashWithAdminCatalog() =>
        Assert.That(CommerceAutomationComponents.All.Select(component => component.Name),
            Has.None.AnyOf(AutomationComponentCatalog.All.Select(component => component.Name).ToArray()));


    [Test]
    public void RegisteredIdentifiers_UsePrefix() =>
        Assert.That(GetRegisteredIdentifiers(), Is.All.StartsWith(AutomationComponentCatalog.IDENTIFIER_PREFIX));


    [Test]
    public void RegistrationAttributes_DisplayNameAndDescriptionKeysExist()
    {
        var keys = GetRegistrationAttributes()
            .SelectMany(attribute => new[]
            {
                (string)attribute.GetType().GetProperty("DisplayName")!.GetValue(attribute)!,
                (string)attribute.GetType().GetProperty("Description")!.GetValue(attribute)!
            })
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(keys, Is.All.StartsWith(KEY_PREFIX));
            AssertKeysExist(keys);
        });
    }


    [Test]
    public void PropertyAttributes_MacroKeysExist()
    {
        var keys = assembly.GetTypes()
            .SelectMany(type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(property => property.GetCustomAttributes()))
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
    public void ComponentNames_HaveDisplayNameKeys() =>
        AssertKeysExist(CommerceAutomationComponents.All.Select(component => $"{KEY_PREFIX}{component.Name.ToLowerInvariant()}.displayname").ToList());


    [Test]
    public void LogCategories_FitEventLogSourceColumn() =>
        Assert.That(CommerceAutomationComponents.All.Select(LogCategories.For), Is.All.Length.LessThanOrEqualTo(EVENT_LOG_COLUMN_LENGTH));


    [Test]
    public void LogEvents_FitEventCodeColumnAndDoNotClashWithAdminEvents()
    {
        var commerceEvents = GetEvents(typeof(CommerceLogEvents));
        var adminEvents = GetEvents(typeof(LogEvents));

        Assert.Multiple(() =>
        {
            Assert.That(commerceEvents.Select(eventId => eventId.Name), Is.All.Length.LessThanOrEqualTo(EVENT_LOG_COLUMN_LENGTH));
            Assert.That(commerceEvents.Concat(adminEvents).Select(eventId => eventId.Id), Is.Unique);
            Assert.That(commerceEvents.Concat(adminEvents).Select(eventId => eventId.Name), Is.Unique);
        });
    }


    [Test]
    public void Types_DoNotInjectTypedLoggers()
    {
        var typesWithTypedLoggers = assembly.GetTypes()
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>)))
            .Select(type => type.FullName);

        Assert.That(typesWithTypedLoggers, Is.Empty);
    }


    private static List<EventId> GetEvents(Type type) => type
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (EventId)field.GetValue(null)!)
        .ToList();


    private static void AssertKeysExist(IReadOnlyCollection<string> keys)
    {
        var missing = keys.Where(key => resourceManager.GetString(key, CultureInfo.InvariantCulture) is null).ToList();

        Assert.That(missing, Is.Empty, "Missing keys in AutomationExtensionsCommerceResources.resx.");
    }


    private static List<string> GetRegisteredIdentifiers() => GetRegistrationAttributes()
        .Select(attribute => (string)attribute.GetType().GetProperty("Identifier")!.GetValue(attribute)!)
        .ToList();


    private static List<Attribute> GetRegistrationAttributes()
    {
        var registrationTypes = new[]
        {
            typeof(RegisterAutomationActionAttribute<>),
            typeof(RegisterAutomationConditionAttribute<>),
            typeof(RegisterAutomationTriggerAttribute<>)
        };

        return assembly.GetCustomAttributes()
            .Where(attribute => attribute.GetType().IsGenericType
                && registrationTypes.Contains(attribute.GetType().GetGenericTypeDefinition()))
            .ToList();
    }


    [GeneratedRegex(@"\{\$(?<key>[^$]+)\$\}")]
    private static partial Regex MacroPattern();
}
