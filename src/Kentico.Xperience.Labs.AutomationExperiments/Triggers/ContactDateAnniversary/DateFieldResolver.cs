using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;

using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Resolves configured date fields (<see cref="ContactDateAnniversaryOptions.GetDateFields"/>) against the contact class definition.
/// Only existing fields of the date or date and time type are returned, which also keeps field names safe for SQL expressions.
/// </summary>
internal sealed class DateFieldResolver(
    IOptionsMonitor<AutomationExperimentsOptions> options,
    ILocalizationService localizationService,
    ILoggerFactory loggerFactory)
{
    private static readonly TimeSpan invalidFieldLogInterval = TimeSpan.FromDays(1);


    private readonly IOptionsMonitor<AutomationExperimentsOptions> options = options;
    private readonly ILocalizationService localizationService = localizationService;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.ContactDateAnniversary));


    public IReadOnlyList<ContactDateField> GetFields()
    {
        var formInfo = FormHelper.GetFormInfo(ContactInfo.OBJECT_TYPE, false);
        var fields = new List<ContactDateField>();

        foreach (string fieldName in options.CurrentValue.ContactDateAnniversary.GetDateFields())
        {
            var field = formInfo?.GetFormField(fieldName);
            if (field is null || !IsDateField(field))
            {
                logger.LogWithIntervalPolicy(
                    LoggingIntervalPolicy.OncePerPeriod($"{nameof(DateFieldResolver)}|InvalidField|{fieldName}", invalidFieldLogInterval),
                    l => l.LogWarning(
                        LogEvents.InvalidDateField,
                        "Configured contact date field '{FieldName}' does not exist or is not a date field and is ignored.",
                        fieldName));
                continue;
            }

            fields.Add(new ContactDateField(field.Name, GetCaption(field)));
        }

        return fields;
    }


    private static bool IsDateField(FormFieldInfo field) =>
        string.Equals(field.DataType, FieldDataType.Date, StringComparison.OrdinalIgnoreCase)
        || string.Equals(field.DataType, FieldDataType.DateTime, StringComparison.OrdinalIgnoreCase);


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
