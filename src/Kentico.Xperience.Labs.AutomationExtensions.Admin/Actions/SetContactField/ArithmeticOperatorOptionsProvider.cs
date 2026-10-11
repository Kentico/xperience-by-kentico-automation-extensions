using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Supplies the operators selectable in <see cref="SetContactFieldActionProperties"/>.
/// </summary>
internal sealed class ArithmeticOperatorOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(ArithmeticOperator.Add, "add"),
            CreateItem(ArithmeticOperator.Subtract, "subtract"),
            CreateItem(ArithmeticOperator.Multiply, "multiply"),
            CreateItem(ArithmeticOperator.Divide, "divide"),
            CreateItem(ArithmeticOperator.Modulo, "modulo")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(ArithmeticOperator arithmeticOperator, string keySuffix) => new()
    {
        Value = arithmeticOperator.ToString(),
        Text = localizationService.GetString($"labs.automationextensions.setcontactfield.operator.{keySuffix}")
    };
}
