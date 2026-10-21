using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

/// <summary>
/// Supplies the configured, existing contact groups (<see cref="ContactGroupResolver"/>) to the drop-down of
/// <see cref="ContactGroupMemberTriggerProperties"/>. Only groups scanned by the scheduled task can be selected.
/// </summary>
internal sealed class ContactGroupOptionsProvider(
    ContactGroupResolver contactGroupResolver,
    ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ContactGroupResolver contactGroupResolver = contactGroupResolver;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var resolution = await contactGroupResolver.Resolve(CancellationToken.None);
        string format = localizationService.GetString("labs.automationextensions.contactgroupmember.contactgroup.format");

        return resolution.Groups
            .Select(group => new DropDownOptionItem
            {
                Value = group.Name,
                Text = string.Format(CultureInfo.CurrentCulture, format, group.DisplayName, group.Name)
            })
            .ToList();
    }
}
