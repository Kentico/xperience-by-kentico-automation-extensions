namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

/// <summary>
/// Property validation and value calculation of <see cref="SetContactFieldAction"/>, independent of the platform.
/// </summary>
internal static class SetContactFieldEvaluator
{
    public const string MESSAGE_UPDATED = "Updated";
    public const string MESSAGE_UNCHANGED = "Unchanged";
    public const string MESSAGE_NOT_EMPTY = "Field not empty, not changed";
    public const string MESSAGE_INVALID_CONFIGURATION = "Invalid configuration";
    public const string MESSAGE_CONVERSION_FAILED = "Value can't be converted";
    public const string MESSAGE_CALCULATION_FAILED = "Calculation failed";
    public const string MESSAGE_SAVE_FAILED = "Save failed";


    /// <summary>
    /// Parses a mode persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseMode(string? value, out SetContactFieldMode mode) => TryParseName(value, out mode);


    /// <summary>
    /// Parses an operator persisted by name (exact, case-sensitive). Numeric values are rejected.
    /// </summary>
    public static bool TryParseOperator(string? value, out ArithmeticOperator arithmeticOperator) => TryParseName(value, out arithmeticOperator);


    /// <summary>
    /// Returns why the properties can't be applied, or <c>null</c> when they are valid.
    /// </summary>
    /// <param name="properties">Step properties.</param>
    /// <param name="field">The resolved changeable field, or <c>null</c> when <see cref="SetContactFieldActionProperties.Field"/> doesn't resolve.</param>
    /// <param name="mode">Parsed mode when valid.</param>
    /// <param name="arithmeticOperator">Parsed operator when valid (only meaningful for <see cref="SetContactFieldMode.Calculate"/>).</param>
    public static string? GetConfigurationError(
        SetContactFieldActionProperties properties,
        ContactField? field,
        out SetContactFieldMode mode,
        out ArithmeticOperator arithmeticOperator)
    {
        mode = default;
        arithmeticOperator = default;

        if (string.IsNullOrWhiteSpace(properties.Field))
        {
            return "no field is selected";
        }

        if (field is null)
        {
            return $"field '{properties.Field}' doesn't exist or can't be changed";
        }

        if (!TryParseMode(properties.Mode, out mode))
        {
            return $"mode '{properties.Mode}' is unknown";
        }

        if (mode != SetContactFieldMode.Calculate)
        {
            return null;
        }

        if (!field.IsNumber)
        {
            return $"field '{field.Name}' isn't a number field, so it can't be calculated";
        }

        if (!TryParseOperator(properties.Operator, out arithmeticOperator))
        {
            return $"operator '{properties.Operator}' is unknown";
        }

        if (string.IsNullOrWhiteSpace(properties.Operand))
        {
            return "no operand is entered";
        }

        return null;
    }


    /// <summary>
    /// Calculates what to do with the field of one contact. Doesn't change anything.
    /// </summary>
    /// <param name="properties">Validated step properties (see <see cref="GetConfigurationError"/>).</param>
    /// <param name="field">Field to change.</param>
    /// <param name="mode">Parsed mode.</param>
    /// <param name="arithmeticOperator">Parsed operator (<see cref="SetContactFieldMode.Calculate"/> only).</param>
    /// <param name="currentValue">Current stored value (<c>null</c> or <see cref="DBNull"/> when empty).</param>
    /// <param name="tokens">Token values for the contact.</param>
    public static SetContactFieldOutcome Evaluate(
        SetContactFieldActionProperties properties,
        ContactField field,
        SetContactFieldMode mode,
        ArithmeticOperator arithmeticOperator,
        object? currentValue,
        ContactFieldTokenValues tokens)
    {
        var unresolvedTokens = new List<string>();

        if (mode == SetContactFieldMode.Calculate)
        {
            string operand = ContactFieldValueTemplate.Resolve(properties.Operand, tokens, unresolvedTokens);
            var calculation = ContactFieldCalculator.Calculate(currentValue, arithmeticOperator, operand, field);

            return calculation.Succeeded
                ? CreateWriteOutcome(currentValue, calculation.Value, unresolvedTokens, truncated: false)
                : SetContactFieldOutcome.Failed(MESSAGE_CALCULATION_FAILED, calculation.Error!, unresolvedTokens);
        }

        if (properties.OnlyIfEmpty && !ContactFieldValueConverter.IsEmpty(currentValue))
        {
            return new SetContactFieldOutcome(SetContactFieldOutcomeKind.NotEmpty, null, MESSAGE_NOT_EMPTY, null, unresolvedTokens, false);
        }

        string text = ContactFieldValueTemplate.Resolve(properties.ValueTemplate, tokens, unresolvedTokens);
        var conversion = ContactFieldValueConverter.Convert(text, field);

        return conversion.Succeeded
            ? CreateWriteOutcome(currentValue, conversion.Value, unresolvedTokens, conversion.Truncated)
            : SetContactFieldOutcome.Failed(MESSAGE_CONVERSION_FAILED, conversion.Error!, unresolvedTokens);
    }


    /// <summary>
    /// Returns whether the stored value already equals the new value (both empty counts as equal).
    /// </summary>
    public static bool AreEqual(object? currentValue, object? newValue)
    {
        bool currentEmpty = ContactFieldValueConverter.IsEmpty(currentValue);
        bool newEmpty = ContactFieldValueConverter.IsEmpty(newValue);
        if (currentEmpty || newEmpty)
        {
            return currentEmpty && newEmpty;
        }

        return Equals(currentValue, newValue);
    }


    private static SetContactFieldOutcome CreateWriteOutcome(object? currentValue, object? newValue, IReadOnlyList<string> unresolvedTokens, bool truncated) =>
        AreEqual(currentValue, newValue)
            ? new SetContactFieldOutcome(SetContactFieldOutcomeKind.Unchanged, newValue, MESSAGE_UNCHANGED, null, unresolvedTokens, truncated)
            : new SetContactFieldOutcome(SetContactFieldOutcomeKind.Write, newValue, MESSAGE_UPDATED, null, unresolvedTokens, truncated);


    private static bool TryParseName<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        result = default;

        return value is not null
            && Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal)
            && Enum.TryParse(value, out result);
    }
}


/// <summary>
/// What <see cref="SetContactFieldAction"/> does with the field of one contact.
/// </summary>
internal enum SetContactFieldOutcomeKind
{
    /// <summary>Save <see cref="SetContactFieldOutcome.NewValue"/>.</summary>
    Write,

    /// <summary>The field already has the new value – nothing to save.</summary>
    Unchanged,

    /// <summary>"Only if empty" is on and the field has a value – nothing to save.</summary>
    NotEmpty,

    /// <summary>The value can't be built – nothing to save, step fails.</summary>
    Failed
}


/// <summary>
/// Result of <see cref="SetContactFieldEvaluator.Evaluate"/>.
/// </summary>
/// <param name="Kind">What to do.</param>
/// <param name="NewValue">New field value (<c>null</c> clears the field).</param>
/// <param name="Message">Short, non-personal step result message.</param>
/// <param name="Error">Reason of a failure, without values.</param>
/// <param name="UnresolvedTokens">Tokens replaced by empty text.</param>
/// <param name="Truncated">Whether the text was shortened to the field size.</param>
internal sealed record SetContactFieldOutcome(
    SetContactFieldOutcomeKind Kind,
    object? NewValue,
    string Message,
    string? Error,
    IReadOnlyList<string> UnresolvedTokens,
    bool Truncated)
{
    public bool Succeeded => Kind != SetContactFieldOutcomeKind.Failed;


    public static SetContactFieldOutcome Failed(string message, string error, IReadOnlyList<string> unresolvedTokens) =>
        new(SetContactFieldOutcomeKind.Failed, null, message, error, unresolvedTokens, false);
}
