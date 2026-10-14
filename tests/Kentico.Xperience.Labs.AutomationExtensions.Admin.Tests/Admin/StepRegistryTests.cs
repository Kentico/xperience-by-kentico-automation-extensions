using Kentico.Xperience.Labs.AutomationExtensions.Admin;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Admin;

public class StepRegistryTests
{
    [Test]
    public void DefaultConfiguration_AllLibraryRegistrationsDisabled()
    {
        var rows = BuildRows(new AutomationExtensionsOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(AutomationComponentCatalog.All.Count));
            Assert.That(rows.Select(row => row.RowId), Is.Unique);
            Assert.That(rows.Select(row => row.Status), Is.All.EqualTo("disabled"));
            Assert.That(rows.Select(row => row.Identifier), Is.EquivalentTo(AutomationComponentCatalog.All.Select(component => component.Identifier)));
            Assert.That(rows.Select(row => row.Description), Is.All.Not.Null);
        });
    }


    [TestCase(false, true)]
    [TestCase(true, false)]
    public void ExplicitConfiguration_OverridesDefaultCaseInsensitively(bool enabledByDefault, bool configuredEnabled)
    {
        var options = new AutomationExtensionsOptions
        {
            EnabledByDefault = enabledByDefault,
            Components = new(StringComparer.OrdinalIgnoreCase) { ["randomsplit"] = configuredEnabled }
        };
        var rows = BuildRows(options);
        var row = rows.Single(row => row.Identifier == AutomationComponentCatalog.RandomSplit.Identifier);

        Assert.Multiple(() =>
        {
            Assert.That(row.Status, Is.EqualTo(configuredEnabled ? "enabled" : "disabled"));
            Assert.That(rows.Where(other => other != row).Select(other => other.Status),
                Is.All.EqualTo(enabledByDefault ? "enabled" : "disabled"));
        });
    }


    [Test]
    public void DefaultEnabled_EnablesAllLibraryComponents()
    {
        var rows = BuildRows(new AutomationExtensionsOptions { EnabledByDefault = true });

        Assert.That(rows.Select(row => row.Status), Is.All.EqualTo("enabled"));
    }


    [Test]
    public void RegistrationIdentifierCase_MatchesPlatformCaseInsensitiveStoreIdentity()
    {
        var registrations = LibraryRegistrations()
            .Select(registration => registration with { Identifier = registration.Identifier.ToLowerInvariant() });
        var rows = BuildRows(new AutomationExtensionsOptions(), registrations);

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(AutomationComponentCatalog.All.Count));
            Assert.That(rows.Select(row => row.Status), Is.All.EqualTo("disabled"));
        });
    }


    [TestCase(AutomationComponentKind.Trigger)]
    [TestCase(AutomationComponentKind.Action)]
    [TestCase(AutomationComponentKind.Condition)]
    public void ThirdPartyRegistration_NotManagedEvenWithMatchingUnknownOption(AutomationComponentKind kind)
    {
        var registration = new RegisteredAutomationComponent("Acme.Custom", "Custom step", "Does custom work.", kind);
        var options = new AutomationExtensionsOptions { Components = { ["Acme.Custom"] = false } };
        var rows = BuildRows(options, [registration, .. LibraryRegistrations()]);
        var row = rows.Single(row => row.Identifier == registration.Identifier);
        var unknown = rows.Single(row => row.Kind is null);

        Assert.Multiple(() =>
        {
            Assert.That(row.Kind, Is.EqualTo(kind));
            Assert.That(row.Name, Is.EqualTo("Custom step"));
            Assert.That(row.Status, Is.EqualTo("notmanaged"));
            Assert.That(row.Description, Is.EqualTo("Does custom work."));
            Assert.That(unknown.Name, Is.EqualTo("Acme.Custom"));
            Assert.That(unknown.Identifier, Is.Null);
            Assert.That(unknown.Status, Is.EqualTo("notapplicable"));
        });
    }


    [Test]
    public void UnknownLibraryPrefix_NotAssumedManaged()
    {
        var registration = new RegisteredAutomationComponent("Labs.AutomationExtensions.Other", "Other", null, AutomationComponentKind.Action);
        var row = BuildRows(new AutomationExtensionsOptions(), [registration])
            .Single(row => row.Identifier == registration.Identifier);

        Assert.That(row.Status, Is.EqualTo("notmanaged"));
    }


    [Test]
    public void IdentifierWithWrongKind_NotAssumedManaged()
    {
        var registration = new RegisteredAutomationComponent(AutomationComponentCatalog.SendWebhook.Identifier, "Other", null, AutomationComponentKind.Condition);
        var rows = BuildRows(new AutomationExtensionsOptions(), [registration]);

        Assert.Multiple(() =>
        {
            Assert.That(rows.Single(row => row.Kind == AutomationComponentKind.Condition && row.Name == "Other").Status, Is.EqualTo("notmanaged"));
            Assert.That(rows.Single(row => row.Kind == AutomationComponentKind.Action && row.Identifier == registration.Identifier).Status, Is.EqualTo("notapplicable"));
            Assert.That(rows.Select(row => row.RowId), Is.Unique);
        });
    }


    [Test]
    public void UnknownConfiguration_RemainsVisibleWithoutMisleadingStatus()
    {
        var options = new AutomationExtensionsOptions { Components = { ["Typo"] = true } };
        var rows = BuildRows(options);
        var row = rows.Single(row => row.Kind is null);

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(AutomationComponentCatalog.All.Count + 1));
            Assert.That(row.Name, Is.EqualTo("Typo"));
            Assert.That(row.Kind, Is.Null);
            Assert.That(row.Status, Is.EqualTo("notapplicable"));
            Assert.That(row.Description, Is.Null);
        });
    }


    [Test]
    public void MissingRegistrations_CatalogEntriesRemainVisibleWithoutRuntimeClaim()
    {
        var rows = BuildRows(new AutomationExtensionsOptions(), []);

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(AutomationComponentCatalog.All.Count));
            Assert.That(rows.Select(row => row.Status), Is.All.EqualTo("notapplicable"));
        });
    }


    [Test]
    public void Status_IsResolvedByExistingServiceNotRecalculatedFromOptions()
    {
        var status = new RecordingStatus();
        var rows = StepRegistry.BuildRows(LibraryRegistrations(), new AutomationExtensionsOptions(), status);

        Assert.Multiple(() =>
        {
            Assert.That(rows.Select(row => row.Status), Is.All.EqualTo("enabled"));
            Assert.That(status.Requested, Is.EquivalentTo(AutomationComponentCatalog.All));
        });
    }


    [Test]
    public void GetRows_ReadsCurrentOptionsAndRegistrationsEachTime()
    {
        var options = new TestOptionsMonitor<AutomationExtensionsOptions>(new());
        var status = new AutomationComponentStatus(options);
        var registrations = LibraryRegistrations().ToList();
        var registry = new StepRegistry(status, options, () => registrations);
        var first = registry.GetRows();
        options.CurrentValue = new AutomationExtensionsOptions { Components = { ["SendWebhook"] = true, ["Unknown"] = false } };
        registrations.Add(new RegisteredAutomationComponent("Acme.New", "New step", null, AutomationComponentKind.Action));
        var second = registry.GetRows();

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(row => row.Status), Is.All.EqualTo("disabled"));
            Assert.That(second.Single(row => row.Identifier == AutomationComponentCatalog.SendWebhook.Identifier).Status, Is.EqualTo("enabled"));
            Assert.That(second, Has.Count.EqualTo(first.Count + 2));
        });
    }


    [Test]
    public void AdditionalComponents_ShownAsLibraryRowsWithStatus()
    {
        var extra = new AutomationComponentDescriptor("OrderCompleted", AutomationComponentKind.Trigger);
        var options = new TestOptionsMonitor<AutomationExtensionsOptions>(new AutomationExtensionsOptions { Components = { ["OrderCompleted"] = true } });
        var registrations = LibraryRegistrations()
            .Append(new RegisteredAutomationComponent(extra.Identifier, "Order completed", null, AutomationComponentKind.Trigger))
            .ToList();
        var registry = new StepRegistry(new AutomationComponentStatus(options), options, () => registrations, [extra]);

        var rows = registry.GetRows();
        var row = rows.Single(row => row.Identifier == extra.Identifier);

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(AutomationComponentCatalog.All.Count + 1));
            Assert.That(row.Status, Is.EqualTo("enabled"));
            Assert.That(rows.Where(other => other.Kind is null), Is.Empty, "Configured extra component isn't reported as unknown.");
        });
    }


    [Test]
    public void AdditionalComponents_NotRegistered_RemainVisible()
    {
        var extra = new AutomationComponentDescriptor("OrderCompleted", AutomationComponentKind.Trigger);
        var options = new AutomationExtensionsOptions();

        var rows = StepRegistry.BuildRows(LibraryRegistrations(), options,
            new AutomationComponentStatus(new TestOptionsMonitor<AutomationExtensionsOptions>(options)),
            AutomationComponentCatalog.WithAdditional([extra]));

        Assert.That(rows.Single(row => row.Identifier == extra.Identifier).Status, Is.EqualTo("notapplicable"));
    }


    internal static IReadOnlyList<StepRegistryRow> BuildRows(
        AutomationExtensionsOptions options,
        IEnumerable<RegisteredAutomationComponent>? registrations = null) =>
        StepRegistry.BuildRows(registrations ?? LibraryRegistrations(), options,
            new AutomationComponentStatus(new TestOptionsMonitor<AutomationExtensionsOptions>(options)));


    internal static IEnumerable<RegisteredAutomationComponent> LibraryRegistrations() =>
        AutomationComponentCatalog.All.Select(component => new RegisteredAutomationComponent(
            component.Identifier,
            $"labs.automationextensions.{component.Name.ToLowerInvariant()}.displayname",
            $"labs.automationextensions.{component.Name.ToLowerInvariant()}.description",
            component.Kind));


    private sealed class RecordingStatus : IAutomationComponentStatus
    {
        public List<AutomationComponentDescriptor> Requested { get; } = [];


        public bool IsEnabled(AutomationComponentDescriptor component)
        {
            Requested.Add(component);
            return true;
        }
    }
}
