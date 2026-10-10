using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Supplies the text match operators of all detail filters in <see cref="ActivityFrequencyProperties"/>.
/// </summary>
internal sealed class TextMatchOperatorOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items = Enum.GetValues<TextMatchOperator>()
            .Select(textOperator => new DropDownOptionItem
            {
                Value = textOperator.ToString(),
                Text = localizationService.GetString($"labs.automationextensions.activityfrequency.textoperator.{textOperator.ToString().ToLowerInvariant()}")
            })
            .ToList();

        return Task.FromResult(items);
    }
}
