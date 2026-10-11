using System.Reflection;

using CMS.Automation;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Configuration;

public class AutomationComponentCatalogTests
{
    [Test]
    public void All_NamesAreUnique()
    {
        var names = AutomationComponentCatalog.All.Select(component => component.Name);

        Assert.That(names, Is.Unique);
    }


    [Test]
    public void All_MatchesRegisteredComponents()
    {
        var registeredIdentifiers = GetRegisteredComponentIdentifiers();
        var catalogIdentifiers = AutomationComponentCatalog.All.Select(component => component.Identifier);

        Assert.That(catalogIdentifiers, Is.EquivalentTo(registeredIdentifiers));
    }


    [Test]
    public void All_IdentifiersUsePrefix()
    {
        var identifiers = GetRegisteredComponentIdentifiers();

        Assert.That(identifiers, Is.All.StartsWith(AutomationComponentCatalog.IDENTIFIER_PREFIX));
    }


    internal static IReadOnlyList<string> GetRegisteredComponentIdentifiers() =>
        GetRegistrationAttributes()
            .Select(attribute => (string)attribute.GetType().GetProperty("Identifier")!.GetValue(attribute)!)
            .ToList();


    internal static IReadOnlyList<Attribute> GetRegistrationAttributes()
    {
        var registrationTypes = new[]
        {
            typeof(RegisterAutomationActionAttribute<>),
            typeof(RegisterAutomationConditionAttribute<>),
            typeof(RegisterAutomationTriggerAttribute<>)
        };

        return typeof(AutomationComponentCatalog).Assembly
            .GetCustomAttributes()
            .Where(attribute => attribute.GetType().IsGenericType
                && registrationTypes.Contains(attribute.GetType().GetGenericTypeDefinition()))
            .ToList();
    }
}
