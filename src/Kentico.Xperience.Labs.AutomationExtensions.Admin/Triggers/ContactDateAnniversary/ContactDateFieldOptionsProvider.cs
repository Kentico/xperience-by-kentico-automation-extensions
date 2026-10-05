using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Supplies the configured contact date fields to the date field drop-down of <see cref="ContactDateAnniversaryTriggerProperties"/>.
/// </summary>
internal sealed class ContactDateFieldOptionsProvider(DateFieldResolver fieldResolver) : IDropDownOptionsProvider
{
    private readonly DateFieldResolver fieldResolver = fieldResolver;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = fieldResolver
            .GetFields()
            .Select(field => new DropDownOptionItem
            {
                Value = field.Name,
                Text = field.Caption
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
