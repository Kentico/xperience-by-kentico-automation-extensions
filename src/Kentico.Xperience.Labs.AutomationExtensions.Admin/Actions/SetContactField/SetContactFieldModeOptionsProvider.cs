using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Supplies the modes selectable in <see cref="SetContactFieldActionProperties"/>.
/// </summary>
internal sealed class SetContactFieldModeOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(SetContactFieldMode.SetValue, "setvalue"),
            CreateItem(SetContactFieldMode.Calculate, "calculate")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(SetContactFieldMode mode, string keySuffix) => new()
    {
        Value = mode.ToString(),
        Text = localizationService.GetString($"labs.automationextensions.setcontactfield.mode.{keySuffix}")
    };
}
