using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Finds contact fields usable as a score: custom integer fields of the contact class definition.
/// Validating stored field names against the definition also keeps them safe as column names.
/// </summary>
/// <remarks>
/// Built-in contact fields are marked as system fields in the class definition (<see cref="FieldBase{FieldType}.System"/>),
/// including integer ID references such as <c>ContactOwnerUserID</c> or <c>ContactCountryID</c>, which carry no reference metadata.
/// </remarks>
public sealed class ContactScoreFieldResolver
{
    private readonly ILocalizationService localizationService;
    private readonly Func<FormInfo?> getFormInfo;


    public ContactScoreFieldResolver(ILocalizationService localizationService)
        : this(localizationService, () => FormHelper.GetFormInfo(ContactInfo.OBJECT_TYPE, false))
    {
    }


    /// <summary>
    /// Uses the given contact class definition source (tests run without an Xperience container).
    /// </summary>
    internal ContactScoreFieldResolver(ILocalizationService localizationService, Func<FormInfo?> getFormInfo)
    {
        this.localizationService = localizationService;
        this.getFormInfo = getFormInfo;
    }


    /// <summary>
    /// Returns the eligible fields sorted by caption.
    /// </summary>
    public IReadOnlyList<ContactScoreField> GetFields() =>
        GetEligibleFields()
            .Select(field => new ContactScoreField(field.Name, GetCaption(field)))
            .OrderBy(field => field.Caption, StringComparer.CurrentCultureIgnoreCase)
            .ToList();


    /// <summary>
    /// Returns the defined name of the eligible field matching <paramref name="fieldName"/> (case-insensitive), or <c>null</c>.
    /// </summary>
    public string? ResolveFieldName(string? fieldName) =>
        string.IsNullOrWhiteSpace(fieldName)
            ? null
            : GetEligibleFields().FirstOrDefault(field => string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))?.Name;


    internal static bool IsEligible(FormFieldInfo field) =>
        string.Equals(field.DataType, FieldDataType.Integer, StringComparison.OrdinalIgnoreCase)
        && !field.PrimaryKey
        && !field.System
        && string.IsNullOrEmpty(field.ReferenceToObjectType)
        && !field.IsDummyField;


    private IEnumerable<FormFieldInfo> GetEligibleFields() =>
        getFormInfo()?.GetFields<FormFieldInfo>().Where(IsEligible) ?? [];


    private string GetCaption(FormFieldInfo field)
    {
        string caption = field.Caption;
        if (string.IsNullOrEmpty(caption))
        {
            return field.Name;
        }

        return localizationService.LocalizeString(caption);
    }
}
