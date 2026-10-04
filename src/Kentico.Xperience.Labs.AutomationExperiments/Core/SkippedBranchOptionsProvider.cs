using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Supplies the branches a skipped condition can follow.
/// </summary>
internal sealed class SkippedBranchOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            new DropDownOptionItem { Value = SkippableStep.BRANCH_NO, Text = localizationService.GetString("labs.automationexperiments.skippedbranch.no") },
            new DropDownOptionItem { Value = SkippableStep.BRANCH_YES, Text = localizationService.GetString("labs.automationexperiments.skippedbranch.yes") }
        ];

        return Task.FromResult(items);
    }
}
