using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

internal sealed record RegisteredAutomationComponent(
    string Identifier,
    string DisplayName,
    string? Description,
    AutomationComponentKind Kind);
