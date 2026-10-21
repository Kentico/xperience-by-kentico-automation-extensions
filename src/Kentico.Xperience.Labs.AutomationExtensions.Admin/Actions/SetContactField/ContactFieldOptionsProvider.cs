using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Supplies the changeable contact fields to the field drop-down of <see cref="SetContactFieldActionProperties"/>.
/// Custom fields come first; system fields are marked.
/// </summary>
internal sealed class ContactFieldOptionsProvider(ContactFieldResolver fieldResolver, ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ContactFieldResolver fieldResolver = fieldResolver;
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        string systemFormat = localizationService.GetString("labs.automationextensions.setcontactfield.field.systemformat");

        var items = fieldResolver
            .GetFields()
            .Select(field => new DropDownOptionItem
            {
                Value = field.Name,
                Text = GetText(field, systemFormat)
            })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }


    /// <summary>
    /// Returns "Caption (CodeName)" (or the code name when the field has no caption), formatted with
    /// <paramref name="systemFormat"/> for system fields.
    /// </summary>
    internal static string GetText(ContactField field, string systemFormat)
    {
        string text = string.Equals(field.Caption, field.Name, StringComparison.Ordinal)
            ? field.Name
            : $"{field.Caption} ({field.Name})";

        return field.IsSystem ? string.Format(CultureInfo.CurrentCulture, systemFormat, text) : text;
    }
}
