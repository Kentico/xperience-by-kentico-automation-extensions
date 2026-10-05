using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Supplies the operations selectable in <see cref="AdjustContactScoreActionProperties"/>.
/// </summary>
internal sealed class ContactScoreOperationOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(ContactScoreOperation.Add, "add"),
            CreateItem(ContactScoreOperation.Subtract, "subtract"),
            CreateItem(ContactScoreOperation.Set, "set")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(ContactScoreOperation operation, string keySuffix) => new()
    {
        Value = operation.ToString(),
        Text = localizationService.GetString($"labs.automationextensions.adjustcontactscore.operation.{keySuffix}")
    };
}
