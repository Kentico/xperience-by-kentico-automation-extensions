using System.Globalization;
using System.Reflection;
using System.Resources;

using CMS.Automation.Internal;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages.Internal;
using Kentico.Xperience.Labs.AutomationExtensions.Admin;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Admin;

public class StepRegistryPageTests
{
    [Test]
    public void Registration_DiscoverableListingTabAfterAllStockTabs()
    {
        var assembly = typeof(StepRegistryPage).Assembly;
        var registration = assembly.GetCustomAttributes<UIPageAttribute>().Single(attribute => attribute.Type == typeof(StepRegistryPage));
        var contacts = typeof(AutomationProcessContactsTab).Assembly.GetCustomAttributes<UIPageAttribute>()
            .Single(attribute => attribute.Type == typeof(AutomationProcessContactsTab));
        var stockPages = typeof(AutomationProcessContactsTab).Assembly.GetCustomAttributes<UIPageAttribute>()
            .Where(attribute => attribute.ParentType == contacts.ParentType);

        Assert.Multiple(() =>
        {
            Assert.That(registration.ParentType, Is.EqualTo(typeof(AutomationProcessEditSection)));
            Assert.That(registration.ParentType, Is.EqualTo(contacts.ParentType));
            Assert.That(contacts.Order, Is.EqualTo(300));
            Assert.That(registration.Order, Is.EqualTo(1000));
            Assert.That(registration.Icon, Is.EqualTo(Icons.ClipboardChecklist));
            Assert.That(stockPages.All(attribute => attribute.Order < registration.Order), Is.True);
            Assert.That(registration.Slug, Is.EqualTo("step-registry"));
            Assert.That(registration.TemplateName, Is.EqualTo(TemplateNames.LISTING));
            Assert.That(registration.Name, Is.EqualTo("{$labs.automationextensions.stepregistry.caption$}"));
            Assert.That(assembly.GetCustomAttributes().Any(attribute => attribute.GetType().Name == "AssemblyDiscoverableAttribute"), Is.True);
            Assert.That(typeof(StepRegistryPage).IsSubclassOf(typeof(DataContainerListingPage)), Is.True);
            Assert.That(new TestLocalization().GetString("labs.automationextensions.stepregistry.caption"), Is.EqualTo("Step registry"));
        });
    }


    [TestCase(typeof(ICustomAutomationActionProvider), typeof(CustomAutomationActionDefinition))]
    [TestCase(typeof(ICustomAutomationConditionProvider), typeof(CustomAutomationConditionDefinition))]
    [TestCase(typeof(ICustomAutomationTriggerProvider), typeof(AutomationTriggerDefinition))]
    public void ProviderApi_InjectedInterfaceAndTypedEnumerable(Type providerType, Type definitionType) => Assert.Multiple(() =>
                                                                                                        {
                                                                                                            Assert.That(providerType.IsPublic, Is.True);
                                                                                                            Assert.That(providerType.IsInterface, Is.True);
                                                                                                            Assert.That(typeof(StepRegistryPage).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType), Does.Contain(providerType));
                                                                                                            Assert.That(providerType.GetMethod("GetAll", Type.EmptyTypes)!.ReturnType,
                                                                                                                Is.EqualTo(typeof(IEnumerable<>).MakeGenericType(definitionType)));
                                                                                                            Assert.That(definitionType.GetProperty("Identifier"), Is.Not.Null);
                                                                                                            Assert.That(definitionType.GetProperty("DisplayName"), Is.Not.Null);
                                                                                                            Assert.That(definitionType.GetProperty("Description"), Is.Not.Null);
                                                                                                        });


    [Test]
    public async Task ConfigurePage_LocalizedReadOnlyColumnsAndGlobalDisabledCallouts()
    {
        var page = CreatePage();
        await page.ConfigurePage();
        await page.ConfigurePage();
        var configuration = page.PageConfiguration;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.FilterConfiguration.FormModel, Is.TypeOf<StepRegistryFilterModel>());
            Assert.That(((StepRegistryFilterModel)configuration.FilterConfiguration.FormModel).Kind, Is.Null);
            Assert.That(((StepRegistryFilterModel)configuration.FilterConfiguration.FormModel).Status, Is.Null);
            Assert.That(configuration.ColumnConfigurations.Select(column => column.Name), Is.EqualTo(new[]
            {
                "Name", "Kind", "Identifier", "Status", "Description"
            }));
            Assert.That(configuration.ColumnConfigurations.Select(column => column.Caption),
                Is.EqualTo(new[] { "Name", "Kind", "Identifier", "Status", "Description" }));
            Assert.That(typeof(StepRegistryRow).GetProperties().Select(property => property.Name),
                Is.EqualTo(new[] { "RowId", "Name", "Kind", "Identifier", "Status", "Description" }));
            Assert.That(configuration.TableActions, Is.Empty);
            Assert.That(configuration.HeaderActions, Is.Empty);
            Assert.That(configuration.MassActions, Is.Empty);
            Assert.That(configuration.RowAction, Is.Null);
            Assert.That(configuration.ExportConfiguration, Is.Null);
            Assert.That(configuration.Callouts, Has.Count.EqualTo(2));
            Assert.That(configuration.Callouts.Select(callout => callout.Placement), Is.All.EqualTo(CalloutPlacement.OnPaper));
            Assert.That(configuration.Callouts.Select(callout => callout.ContentAsHtml), Is.All.True);
            Assert.That(configuration.Callouts.Select(callout => callout.Content), Is.All.Contains("<br>"));
            Assert.That(configuration.Callouts[0].Content, Does.Contain("whole application").And.Contain("not just this process"));
            Assert.That(configuration.ColumnConfigurations.Single(column => column.Name == "Status").Sorting.DefaultDirection, Is.EqualTo(SortTypeEnum.Asc));
            Assert.That(configuration.Callouts[1].Content, Does.Contain("all contacts follow the \"No\" path without evaluating the condition")
                .And.Contain("overrides marketer skip settings")
                .And.Contain("actions do nothing").And.Contain("triggers cannot start"));
        });
    }


    [Test]
    public void FilterModel_LocalizedOptionsMatchDisplayedKindsAndStatuses()
    {
        var localization = new TestLocalization();
        var properties = typeof(StepRegistryFilterModel).GetProperties();
        var parser = typeof(DataContainerListingPage).Assembly
            .GetType("Kentico.Xperience.Admin.Base.Forms.DropDownOptionsParser")!
            .GetMethod("ParseDataSourceToOptions", BindingFlags.Public | BindingFlags.Static)!;
        string[][] expectedLabels =
        {
            new[] { "Trigger", "Condition", "Action", "Unknown" },
            new[] { "Enabled", "Disabled", "Not managed", "Not applicable" }
        };
        string[][] expectedValues =
        {
            new[] { "trigger", "condition", "action", "unknown" },
            new[] { "enabled", "disabled", "notmanaged", "notapplicable" }
        };

        Assert.Multiple(() =>
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var component = properties[i].GetCustomAttribute<DropDownComponentAttribute>()!;
                var options = ((IEnumerable<DropDownOptionItem>)parser.Invoke(null,
                    [component.Options, ";", localization])!).ToList();

                Assert.That(component.Order, Is.EqualTo(i));
                Assert.That(localization.LocalizeString(component.Label), Is.EqualTo(i == 0 ? "Kind" : "Status"));
                Assert.That(localization.LocalizeString(component.Placeholder), Is.EqualTo("All"));
                Assert.That(options.Select(option => option.Text), Is.EqualTo(expectedLabels[i]));
                Assert.That(options.Select(option => option.Value), Is.EqualTo(expectedValues[i]));
                Assert.That(properties[i].GetCustomAttribute<FilterConditionAttribute>()!.BuilderType,
                    Is.EqualTo(typeof(StepRegistryFilterConditionBuilder)));
                Assert.That(properties[i].GetCustomAttribute<FilterIgnoreAttribute>(), Is.Null);
            }
        });
    }


    [Test]
    public async Task FilterConditionBuilder_InMemoryFilteringDoesNotGenerateSql()
    {
        var condition = await new StepRegistryFilterConditionBuilder().Build("Kind", "trigger");

        Assert.That(condition.WhereIsEmpty, Is.True);
    }


    [TestCase("trigger", null, new[] { "ContactDateAnniversary", "Acme.Trigger" })]
    [TestCase("condition", null, new[] { "PreviousStepSucceeded", "RandomSplit", "WithinTimeWindow" })]
    [TestCase("action", null, new[] { "AdjustContactScore", "SendWebhook", "WriteToLog", "Acme.Action" })]
    [TestCase("unknown", null, new[] { "Typo" })]
    [TestCase(null, "enabled", new[] { "AdjustContactScore", "ContactDateAnniversary", "PreviousStepSucceeded", "SendWebhook", "WithinTimeWindow" })]
    [TestCase(null, "disabled", new[] { "RandomSplit" })]
    [TestCase(null, "notmanaged", new[] { "Acme.Action", "Acme.Trigger" })]
    [TestCase(null, "notapplicable", new[] { "WriteToLog", "Typo" })]
    [TestCase("condition", "disabled", new[] { "RandomSplit" })]
    [TestCase("action", "notmanaged", new[] { "Acme.Action" })]
    [TestCase("unknown", "notapplicable", new[] { "Typo" })]
    [TestCase("action", "notapplicable", new[] { "WriteToLog" })]
    [TestCase("unknown", "enabled", new string[0])]
    public async Task LoadListing_FiltersReturnExactRows(string? kind, string? status, string[] expected)
    {
        var page = CreateMixedPage();
        await page.ConfigurePage();
        var filters = (StepRegistryFilterModel)page.PageConfiguration.FilterConfiguration.FormModel;
        filters.Kind = kind;
        filters.Status = status;

        var data = await LoadFilteredListing(page);

        Assert.Multiple(() =>
        {
            Assert.That(data.TotalCount, Is.EqualTo(expected.Length));
            Assert.That(data.DataContainers.Select(row => GetComponentKey((string)row["RowId"])),
                Is.EquivalentTo(expected));
        });
    }


    [Test]
    public async Task LoadListing_CombinedFiltersSearchSortAndPagingCountBeforePagination()
    {
        var page = CreateMixedPage();
        await page.ConfigurePage();
        var filters = (StepRegistryFilterModel)page.PageConfiguration.FilterConfiguration.FormModel;
        filters.Kind = "action";
        filters.Status = "notmanaged";
        var data = await LoadFilteredListing(page, search: "Custom");
        Assert.That(data.DataContainers.Single()["Identifier"], Is.EqualTo("Acme.Action"));

        var noMatch = await LoadFilteredListing(page, search: "Random split");
        Assert.That(noMatch.TotalCount, Is.Zero);
        Assert.That(noMatch.DataContainers, Is.Empty);

        filters.Status = null;
        var first = await LoadFilteredListing(page, pageSize: 1);
        var second = await LoadFilteredListing(page, pageSize: 1, selectedPage: 1);
        var third = await LoadFilteredListing(page, pageSize: 1, selectedPage: 2);
        var fourth = await LoadFilteredListing(page, pageSize: 1, selectedPage: 3);
        var pastEnd = await LoadFilteredListing(page, pageSize: 1, selectedPage: 4);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { first.TotalCount, second.TotalCount, third.TotalCount, fourth.TotalCount, pastEnd.TotalCount }, Is.All.EqualTo(4));
            Assert.That(first.DataContainers.Single()["Identifier"], Is.EqualTo(AutomationComponentCatalog.WriteToLog.Identifier));
            Assert.That(second.DataContainers.Single()["Identifier"], Is.EqualTo(AutomationComponentCatalog.SendWebhook.Identifier));
            Assert.That(third.DataContainers.Single()["Identifier"], Is.EqualTo("Acme.Action"));
            Assert.That(fourth.DataContainers.Single()["Identifier"], Is.EqualTo(AutomationComponentCatalog.AdjustContactScore.Identifier));
            Assert.That(pastEnd.DataContainers, Is.Empty);
        });
    }


    [TestCase(null)]
    [TestCase("")]
    public async Task LoadListing_ResetToAllRestoresRowsAndRetainsSearch(string? all)
    {
        var page = CreateMixedPage();
        await page.ConfigurePage();
        var filters = (StepRegistryFilterModel)page.PageConfiguration.FilterConfiguration.FormModel;
        var before = await LoadFilteredListing(page);
        filters.Kind = "unknown";
        filters.Status = "notapplicable";
        Assert.That((await LoadFilteredListing(page)).TotalCount, Is.EqualTo(1));

        filters.Kind = all;
        filters.Status = all;
        var reset = await LoadFilteredListing(page);
        var searched = await LoadFilteredListing(page, search: "Custom");

        Assert.Multiple(() =>
        {
            Assert.That(reset.TotalCount, Is.EqualTo(10));
            Assert.That(reset.DataContainers.Select(row => row["RowId"]),
                Is.EqualTo(before.DataContainers.Select(row => row["RowId"])));
            Assert.That(searched.TotalCount, Is.EqualTo(2));
            Assert.That(searched.DataContainers.Select(row => row["Identifier"]),
                Is.EquivalentTo(new[] { "Acme.Action", "Acme.Trigger" }));
        });
    }


    private static StepRegistryPage CreateMixedPage()
    {
        var options = new AutomationExtensionsOptions
        {
            EnabledByDefault = true,
            Components = { ["RandomSplit"] = false, ["WriteToLog"] = true, ["Typo"] = false }
        };
        var registrations = StepRegistryTests.LibraryRegistrations()
            .Where(registration => registration.Identifier != AutomationComponentCatalog.WriteToLog.Identifier)
            .Concat(new[]
            {
                new RegisteredAutomationComponent("Acme.Action", "Custom action", "Plain action details.", AutomationComponentKind.Action),
                new RegisteredAutomationComponent("Acme.Trigger", "Custom trigger", null, AutomationComponentKind.Trigger)
            }).ToList();
        return CreatePage(options, () => registrations);
    }


    private static string GetComponentKey(string rowId) =>
        rowId[(rowId.IndexOf(':') + 1)..].Replace(AutomationComponentCatalog.IDENTIFIER_PREFIX, "", StringComparison.Ordinal);


    private static Task<ListingData> LoadFilteredListing(
        StepRegistryPage page, string search = "", int pageSize = 25, int selectedPage = 0) =>
        LoadListing(page,
            new ListingDataSettings(new LoadDataSettings(pageSize, selectedPage, "Name", SortTypeEnum.Desc, search),
                page.PageConfiguration.ColumnConfigurations),
            CancellationToken.None);


    [Test]
    public void Rows_DisplayRequestedColumnsAndLocalizeDescriptions()
    {
        var options = new AutomationExtensionsOptions { EnabledByDefault = true, Components = { ["RandomSplit"] = false, ["Typo"] = true } };
        var registrations = new List<RegisteredAutomationComponent>(StepRegistryTests.LibraryRegistrations())
        {
            new("Acme.Step", "Custom step", "Plain custom description.", AutomationComponentKind.Action)
        };
        var page = CreatePage(options, () => registrations);
        var rows = StepRegistryTests.BuildRows(options, registrations).Select(page.ToDataContainer).ToList();
        var split = rows.Single(row => (string)row["Identifier"] == AutomationComponentCatalog.RandomSplit.Identifier);
        var defaultRow = rows.Single(row => (string)row["Identifier"] == AutomationComponentCatalog.WriteToLog.Identifier);
        var other = rows.Single(row => (string)row["Identifier"] == "Acme.Step");
        var unknown = rows.Single(row => (string)row["Name"] == "Typo");

        Assert.Multiple(() =>
        {
            Assert.That(split["Name"], Is.EqualTo("Random split (A/B)"));
            Assert.That(split["Kind"], Is.EqualTo("Condition"));
            Assert.That(split["Status"], Is.EqualTo("Disabled"));
            Assert.That(split["Description"], Is.EqualTo("Assigns each contact to a stable Yes (A) or No (B) branch. Requires administrator enablement; when disabled by application configuration, all contacts follow No (B), regardless of skip settings."));
            Assert.That(defaultRow["Status"], Is.EqualTo("Enabled"));
            Assert.That(defaultRow["Description"], Is.EqualTo("Writes a message to the Event log for every contact reaching this step. Use to check which path contacts take. Usually enabled only during testing."));
            Assert.That(other["Name"], Is.EqualTo("Custom step"));
            Assert.That(other["Status"], Is.EqualTo("Not managed"));
            Assert.That(other["Description"], Is.EqualTo("Plain custom description."));
            Assert.That(unknown["Kind"], Is.EqualTo("Unknown"));
            Assert.That(unknown["Status"], Is.EqualTo("Not applicable"));
            Assert.That(unknown["Description"], Is.EqualTo(string.Empty));
            Assert.That(rows.Select(row => row["RowId"]), Is.Unique);
        });
    }


    [Test]
    public void MissingRegistration_DisplaysNotApplicableStatusAndEmptyDescription()
    {
        var options = new AutomationExtensionsOptions { Components = { ["SendWebhook"] = true } };
        var page = CreatePage(options);
        var row = StepRegistryTests.BuildRows(options, [])
            .Single(row => row.Identifier == AutomationComponentCatalog.SendWebhook.Identifier);
        var data = page.ToDataContainer(row);

        Assert.Multiple(() =>
        {
            Assert.That(data["Status"], Is.EqualTo("Not applicable"));
            Assert.That(data["Description"], Is.EqualTo(string.Empty));
        });
    }


    [Test]
    public async Task LoadListing_BuiltInSearchSortAndPaginationWorkWithActualRowShape()
    {
        var page = CreatePage();
        await page.ConfigurePage();
        var settings = new LoadDataSettings(1, 0, "Name", SortTypeEnum.Desc, "condition");
        var data = await LoadListing(page,
            new ListingDataSettings(settings, page.PageConfiguration.ColumnConfigurations), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(data.TotalCount, Is.EqualTo(3));
            Assert.That(data.DataContainers.Count(), Is.EqualTo(1));
            Assert.That(data.DataContainers.Single()["Name"], Is.EqualTo("Within time window"));
        });

        var second = await LoadListing(page,
            new ListingDataSettings(new LoadDataSettings(1, 1, "Name", SortTypeEnum.Desc, "condition"), page.PageConfiguration.ColumnConfigurations),
            CancellationToken.None);

        Assert.That(second.DataContainers.Single()["Name"], Is.EqualTo("Random split (A/B)"));
    }


    [Test]
    public async Task LoadListing_CancellationAndStoreFailuresPropagate()
    {
        var page = CreatePage(getRegistrations: () => throw new InvalidOperationException("Registration store failed"));
        await page.ConfigurePage();
        var settings = new ListingDataSettings(new LoadDataSettings(10, 0, "Name", SortTypeEnum.Asc, ""), page.PageConfiguration.ColumnConfigurations);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await LoadListing(page, settings, CancellationToken.None));
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await LoadListing(page, settings, new CancellationToken(true)));
        });
    }


    [TestCase("Status")]
    [TestCase("")]
    public async Task LoadListing_StatusUsesRequestedOrderBeforePaging(string sortBy)
    {
        var options = new AutomationExtensionsOptions { EnabledByDefault = true, Components = { ["RandomSplit"] = false, ["Typo"] = true } };
        var registrations = new List<RegisteredAutomationComponent>(StepRegistryTests.LibraryRegistrations())
        {
            new("Acme.Step", "Custom step", null, AutomationComponentKind.Action)
        };
        var page = CreatePage(options, () => registrations);
        await page.ConfigurePage();
        var data = await LoadListing(page,
            new ListingDataSettings(new LoadDataSettings(100, 0, sortBy, SortTypeEnum.Asc, ""), page.PageConfiguration.ColumnConfigurations),
            CancellationToken.None);

        Assert.That(data.DataContainers.Select(row => row["Status"]),
            Is.EqualTo(new[] { "Enabled", "Enabled", "Enabled", "Enabled", "Enabled", "Enabled", "Disabled", "Not managed", "Not applicable" }));

        var second = await LoadListing(page,
            new ListingDataSettings(new LoadDataSettings(4, 1, "Status", SortTypeEnum.Asc, ""), page.PageConfiguration.ColumnConfigurations),
            CancellationToken.None);
        Assert.That(second.DataContainers.Select(row => row["Status"]),
            Is.EqualTo(new[] { "Enabled", "Enabled", "Disabled", "Not managed" }));
        Assert.That(second.TotalCount, Is.EqualTo(9));
    }


    private static Task<ListingData> LoadListing(StepRegistryPage page, ListingDataSettings settings, CancellationToken cancellationToken) =>
        (Task<ListingData>)typeof(DataContainerListingPage)
            .GetMethod("LoadListingDataContainers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(page, [settings, cancellationToken])!;


    private static StepRegistryPage CreatePage(
        AutomationExtensionsOptions? options = null,
        Func<IEnumerable<RegisteredAutomationComponent>>? getRegistrations = null)
    {
        var monitor = new TestOptionsMonitor<AutomationExtensionsOptions>(options ?? new());
        var registry = new StepRegistry(new AutomationComponentStatus(monitor), monitor,
            getRegistrations ?? StepRegistryTests.LibraryRegistrations);
        return new StepRegistryPage(registry, new TestLocalization());
    }


    private sealed class TestLocalization : ILocalizationService
    {
        private static readonly ResourceManager resources = new(typeof(AutomationExtensionsResources));


        public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true)
        {
            string? value = resources.GetString(resourceKey, CultureInfo.InvariantCulture);
            if (value is null && resourceKey.StartsWith("labs.automationextensions.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Missing localization key: {resourceKey}");
            }

            return value ?? resourceKey;
        }


        public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) =>
            inputText.StartsWith("{$", StringComparison.Ordinal) && inputText.EndsWith("$}", StringComparison.Ordinal)
                ? GetString(inputText[2..^2], culture, useDefaultCulture)
                : inputText;


        public string LocalizeExpression(string expression, string? culture = null, bool encode = false,
            Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) => GetString(expression);
    }
}
