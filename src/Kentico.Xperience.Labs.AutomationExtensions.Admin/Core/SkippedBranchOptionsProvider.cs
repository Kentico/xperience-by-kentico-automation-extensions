using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

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
            new DropDownOptionItem { Value = SkippableStep.BRANCH_NO, Text = localizationService.GetString("labs.automationextensions.skippedbranch.no") },
            new DropDownOptionItem { Value = SkippableStep.BRANCH_YES, Text = localizationService.GetString("labs.automationextensions.skippedbranch.yes") }
        ];

        return Task.FromResult(items);
    }
}
