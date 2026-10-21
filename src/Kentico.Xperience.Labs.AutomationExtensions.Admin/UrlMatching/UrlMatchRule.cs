namespace Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

/// <summary>
/// One condition on a URL part, e.g. "path starts with /blog/".
/// </summary>
/// <param name="Part">Compared URL part.</param>
/// <param name="Operator">Comparison; <see cref="UrlMatchOperator.Exists"/> and <see cref="UrlMatchOperator.DoesNotExist"/> only for <see cref="UrlPart.QueryParameter"/>.</param>
/// <param name="Value">Compared text (trimmed); <c>null</c> for operators without a value.</param>
/// <param name="Parameter">Query parameter name (trimmed) for <see cref="UrlPart.QueryParameter"/>; otherwise <c>null</c>.</param>
public sealed record UrlMatchRule(UrlPart Part, UrlMatchOperator Operator, string? Value, string? Parameter = null)
{
    /// <summary>
    /// Returns whether the rule matches only when its part has a matching value – the opposite of the negated operators
    /// (<see cref="UrlMatchOperator.NotEqual"/>, <see cref="UrlMatchOperator.DoesNotContain"/>, <see cref="UrlMatchOperator.DoesNotExist"/>).
    /// </summary>
    public bool IsPositive => Operator is not (UrlMatchOperator.NotEqual or UrlMatchOperator.DoesNotContain or UrlMatchOperator.DoesNotExist);
}
