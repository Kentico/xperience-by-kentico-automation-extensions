using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

/// <summary>
/// Supplies the configured, existing recipient lists (<see cref="RecipientListResolver"/>) to the drop-down of
/// <see cref="RecipientListMemberTriggerProperties"/>. Only lists scanned by the scheduled task can be selected.
/// </summary>
internal sealed class RecipientListOptionsProvider(
    RecipientListResolver recipientListResolver,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly RecipientListResolver recipientListResolver = recipientListResolver;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var resolution = await recipientListResolver.Resolve(CancellationToken.None);
        string format = localizationService.GetString("labs.automationextensions.recipientlistmember.recipientlist.format");

        return resolution.Lists
            .Select(list => new DropDownOptionItem
            {
                Value = list.Name,
                Text = string.Format(CultureInfo.CurrentCulture, format, list.DisplayName, list.Name)
            })
            .ToList();
    }
}
