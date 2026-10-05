using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

internal sealed record StepRegistryRow(
    string RowId,
    string Name,
    AutomationComponentKind? Kind,
    string? Identifier,
    string Status,
    string? Description);
