using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Supplies all registered <see cref="StepResultSource"/> instances (library and custom steps) to the step drop-down
/// of conditions reading results.
/// </summary>
internal sealed class StepResultSourceOptionsProvider(
    IEnumerable<StepResultSource> sources,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly IEnumerable<StepResultSource> sources = sources;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = sources
            .Select(source => new DropDownOptionItem
            {
                Value = source.Name,
                Text = localizationService.GetString(source.DisplayName)
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
