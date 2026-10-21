using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Supplies the eligible integer contact fields to the field drop-down of <see cref="AdjustContactScoreActionProperties"/>.
/// </summary>
internal sealed class ContactScoreFieldOptionsProvider(ContactScoreFieldResolver fieldResolver) : IDropDownOptionsProvider
{
    private readonly ContactScoreFieldResolver fieldResolver = fieldResolver;


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
