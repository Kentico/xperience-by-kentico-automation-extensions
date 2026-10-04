using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Supplies result numbers (1 – <see cref="StepResultKeys.MAXIMUM_RESULT_NUMBER"/>) to "Result number" drop-downs.
/// Use in properties of custom steps that save results.
/// </summary>
public sealed class StepResultNumberOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        string textFormat = localizationService.GetString("labs.automationexperiments.stepresult.number.text");

        var items = Enumerable.Range(1, StepResultKeys.MAXIMUM_RESULT_NUMBER)
            .Select(number => new DropDownOptionItem
            {
                Value = number.ToString(CultureInfo.InvariantCulture),
                Text = string.Format(CultureInfo.CurrentCulture, textFormat, number)
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
