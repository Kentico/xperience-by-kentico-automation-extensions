using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Finds contact fields of the contact class definition that <see cref="SetContactFieldAction"/> can change (system and
/// custom fields) or read in <c>{contact.&lt;Field&gt;}</c> tokens. Validating stored field names against the definition
/// also keeps them safe as column names.
/// </summary>
/// <remarks>
/// Changeable fields are database fields of a supported data type that aren't the primary key and aren't maintained by
/// the system (<see cref="SystemMaintainedFields"/>).
/// </remarks>
public sealed class ContactFieldResolver
{
    /// <summary>
    /// Built-in contact fields maintained by Xperience (identity, timestamps, bounce counter, legacy Salesforce
    /// replication state), foreign keys (status, owner, country, state) and the gender enum. Never offered for writing.
    /// </summary>
    public static readonly IReadOnlySet<string> SystemMaintainedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ContactID",
        "ContactGUID",
        "ContactCreated",
        "ContactLastModified",
        "ContactBounces",
        "ContactStatusID",
        "ContactOwnerUserID",
        "ContactCountryID",
        "ContactStateID",
        "ContactGender",
        "ContactSalesForceLeadID",
        "ContactSalesForceLeadReplicationDisabled",
        "ContactSalesForceLeadReplicationDateTime",
        "ContactSalesForceLeadReplicationSuspensionDateTime",
        "ContactSalesForceLeadReplicationRequired"
    };


    /// <summary>
    /// Data types that can be written.
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedDataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        FieldDataType.Text,
        FieldDataType.LongText,
        FieldDataType.Integer,
        FieldDataType.LongInteger,
        FieldDataType.Decimal,
        FieldDataType.Double,
        FieldDataType.Boolean,
        FieldDataType.Date,
        FieldDataType.DateTime
    };


    private readonly ILocalizationService localizationService;
    private readonly Func<FormInfo?> getFormInfo;


    public ContactFieldResolver(ILocalizationService localizationService)
        : this(localizationService, () => FormHelper.GetFormInfo(ContactInfo.OBJECT_TYPE, false))
    {
    }


    /// <summary>
    /// Uses the given contact class definition source (tests run without an Xperience container).
    /// </summary>
    internal ContactFieldResolver(ILocalizationService localizationService, Func<FormInfo?> getFormInfo)
    {
        this.localizationService = localizationService;
        this.getFormInfo = getFormInfo;
    }


    /// <summary>
    /// Returns the changeable fields: custom fields first, then system fields, each sorted by caption.
    /// </summary>
    public IReadOnlyList<ContactField> GetFields() =>
        GetDefinedFields()
            .Where(IsWritable)
            .Select(ToContactField)
            .OrderBy(field => field.IsSystem)
            .ThenBy(field => field.Caption, StringComparer.CurrentCultureIgnoreCase)
            .ToList();


    /// <summary>
    /// Returns the changeable field matching <paramref name="fieldName"/> (case-insensitive), or <c>null</c>.
    /// </summary>
    public ContactField? Resolve(string? fieldName) => Find(fieldName, IsWritable);


    /// <summary>
    /// Returns the readable field matching <paramref name="fieldName"/> (case-insensitive), or <c>null</c>.
    /// Readable fields are all database fields of the definition, including system-maintained ones.
    /// </summary>
    public ContactField? ResolveReadable(string? fieldName) => Find(fieldName, IsReadable);


    internal static bool IsReadable(FormFieldInfo field) => !field.IsDummyField;


    internal static bool IsWritable(FormFieldInfo field) =>
        IsReadable(field)
        && !field.PrimaryKey
        && !SystemMaintainedFields.Contains(field.Name)
        && string.IsNullOrEmpty(field.ReferenceToObjectType)
        && SupportedDataTypes.Contains(field.DataType);


    private ContactField? Find(string? fieldName, Func<FormFieldInfo, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        var field = GetDefinedFields()
            .FirstOrDefault(field => predicate(field) && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));

        return field is null ? null : ToContactField(field);
    }


    private IEnumerable<FormFieldInfo> GetDefinedFields() => getFormInfo()?.GetFields<FormFieldInfo>() ?? [];


    private ContactField ToContactField(FormFieldInfo field) =>
        new(field.Name, GetCaption(field), field.DataType, field.Size, field.AllowEmpty, field.System);


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
