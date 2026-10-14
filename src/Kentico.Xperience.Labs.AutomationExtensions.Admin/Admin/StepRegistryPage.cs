using CMS.Automation.Internal;
using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalMarketing.UIPages.Internal;
using Kentico.Xperience.Labs.AutomationExtensions.Admin;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

using Microsoft.Extensions.Options;

[assembly: UIPage(
    typeof(AutomationProcessEditSection),
    "step-registry",
    typeof(StepRegistryPage),
    "{$labs.automationextensions.stepregistry.caption$}",
    TemplateNames.LISTING,
    1000,
    Icon = Icons.ClipboardChecklist)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

/// <summary>
/// Displays the global registered automation definitions and library configuration.
/// </summary>
public sealed class StepRegistryPage : DataContainerListingPage
{
    private const string RESOURCE_PREFIX = "labs.automationextensions.stepregistry.";
    private const string STATUS_SORT_FIELD = "StatusSortOrder";

    private readonly StepRegistry registry;
    private readonly ILocalizationService localization;


    public StepRegistryPage(
        IAutomationComponentStatus status,
        IOptionsMonitor<AutomationExtensionsOptions> options,
        ICustomAutomationTriggerProvider triggerProvider,
        ICustomAutomationActionProvider actionProvider,
        ICustomAutomationConditionProvider conditionProvider,
        IEnumerable<AutomationComponentDescriptor> additionalComponents,
        ILocalizationService localization)
        : this(new StepRegistry(status, options, triggerProvider, actionProvider, conditionProvider, additionalComponents), localization)
    {
    }


    internal StepRegistryPage(StepRegistry registry, ILocalizationService localization)
    {
        this.registry = registry;
        this.localization = localization;
    }


    public override Task ConfigurePage()
    {
        PageConfiguration.FilterConfiguration.FormModel = new StepRegistryFilterModel();
        PageConfiguration.ColumnConfigurations.Clear();
        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(StepRegistryRow.Name), Text("name"), searchable: true)
            .AddColumn(nameof(StepRegistryRow.Kind), Text("kind"), searchable: true)
            .AddColumn(nameof(StepRegistryRow.Identifier), Text("identifier"), searchable: true)
            .AddColumn(nameof(StepRegistryRow.Status), Text("status"), searchable: true, defaultSortDirection: SortTypeEnum.Asc)
            .AddColumn(nameof(StepRegistryRow.Description), Text("description"));

        PageConfiguration.Callouts =
        [
            new CalloutConfiguration
            {
                Content = Text("scope.content"),
                ContentAsHtml = true,
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnPaper
            },
            new CalloutConfiguration
            {
                Content = Text("disabled.content"),
                ContentAsHtml = true,
                Type = CalloutType.FriendlyWarning,
                Placement = CalloutPlacement.OnPaper,
            }
        ];

        return base.ConfigurePage();
    }


    protected override Task<IEnumerable<IDataContainer>> LoadDataContainers(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filters = (StepRegistryFilterModel)PageConfiguration.FilterConfiguration.FormModel;
        return Task.FromResult(registry.GetRows()
            .Where(row => (string.IsNullOrEmpty(filters.Kind) || GetKindKey(row) == filters.Kind)
                && (string.IsNullOrEmpty(filters.Status) || row.Status == filters.Status))
            .Select(ToDataContainer));
    }


    protected override object GetIdentifier(IDataContainer dataContainer) => dataContainer[nameof(StepRegistryRow.RowId)];


    protected override Task<ListingData> LoadListingDataContainers(ListingDataSettings settings, CancellationToken cancellationToken)
    {
        var load = settings.LoadSettings;
        if (string.IsNullOrEmpty(load.SortBy) || load.SortBy == nameof(StepRegistryRow.Status))
        {
            var columns = settings.Columns.ToList();
            columns.AddColumn(STATUS_SORT_FIELD, visible: false);
            settings = new ListingDataSettings(
                new LoadDataSettings(load.PageSize, load.SelectedPage, STATUS_SORT_FIELD,
                    string.IsNullOrEmpty(load.SortBy) ? SortTypeEnum.Asc : load.SortType,
                    load.SearchTerm, load.FilterWhereCondition),
                columns)
            {
                UsePagination = settings.UsePagination
            };
        }

        return base.LoadListingDataContainers(settings, cancellationToken);
    }


    internal IDataContainer ToDataContainer(StepRegistryRow row) => new DataContainer
    {
        [nameof(StepRegistryRow.RowId)] = row.RowId,
        [nameof(StepRegistryRow.Name)] = row.Kind is null ? row.Name : LocalizeIfResourceKey(row.Name),
        [nameof(StepRegistryRow.Kind)] = Text(GetKindKey(row)),
        [nameof(StepRegistryRow.Identifier)] = row.Identifier ?? Text("notapplicable"),
        [nameof(StepRegistryRow.Status)] = Text(row.Status),
        [STATUS_SORT_FIELD] = row.Status switch
        {
            "enabled" => 0,
            "disabled" => 1,
            "notmanaged" => 2,
            _ => 3
        },
        [nameof(StepRegistryRow.Description)] = LocalizeIfResourceKey(row.Description)
    };


    private static string GetKindKey(StepRegistryRow row) =>
        row.Kind?.ToString().ToLowerInvariant() ?? "unknown";


    private string LocalizeIfResourceKey(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains('.') && !value.Any(char.IsWhiteSpace)
            ? localization.GetString(value)
            : value;
    }


    private string Text(string suffix) => localization.GetString(RESOURCE_PREFIX + suffix);
}

internal class LabelWithTooltipSubcaption : SubcaptionComponentBase<LabelWithTooltipProperties, LabelWithTooltipClientProperties>
{
    /// <inheritdoc/>
    public override string ComponentName => "@kentico/xperience-admin-components/LabelWithTooltip";


    /// <inheritdoc/>
    protected override Task ConfigureComponentClientProperties(LabelWithTooltipClientProperties componentClientProperties)
    {
        componentClientProperties.Label = ComponentProperties.Label;
        componentClientProperties.TooltipText = ComponentProperties.TooltipText;

        return base.ConfigureComponentClientProperties(componentClientProperties);
    }
}
