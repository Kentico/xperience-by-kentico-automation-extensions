using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ContactNumberFieldCompare;

/// <summary>
/// Supplies the operators selectable in <see cref="ContactNumberFieldCompareProperties"/>.
/// </summary>
internal sealed class NumberComparisonOperatorOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(NumberComparisonOperator.Equal, "equal"),
            CreateItem(NumberComparisonOperator.NotEqual, "notequal"),
            CreateItem(NumberComparisonOperator.GreaterThan, "greaterthan"),
            CreateItem(NumberComparisonOperator.GreaterThanOrEqual, "greaterthanorequal"),
            CreateItem(NumberComparisonOperator.LessThan, "lessthan"),
            CreateItem(NumberComparisonOperator.LessThanOrEqual, "lessthanorequal")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(NumberComparisonOperator comparisonOperator, string keySuffix) => new()
    {
        Value = comparisonOperator.ToString(),
        Text = localizationService.GetString($"labs.automationextensions.contactnumberfieldcompare.operator.{keySuffix}")
    };
}
