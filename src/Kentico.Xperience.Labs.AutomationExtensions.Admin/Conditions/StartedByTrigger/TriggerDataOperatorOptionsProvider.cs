using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Supplies the operators selectable in <see cref="StartedByTriggerProperties.Operator"/>.
/// </summary>
internal sealed class TriggerDataOperatorOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items = Enum.GetValues<TriggerDataOperator>()
            .Select(comparisonOperator => new DropDownOptionItem
            {
                Value = comparisonOperator.ToString(),
                Text = localizationService.GetString($"labs.automationextensions.startedbytrigger.operator.{comparisonOperator.ToString().ToLowerInvariant()}")
            })
            .ToList();

        return Task.FromResult(items);
    }
}
