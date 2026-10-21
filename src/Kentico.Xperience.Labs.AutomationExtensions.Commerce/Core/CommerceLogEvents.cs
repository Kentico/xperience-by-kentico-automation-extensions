using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Event identifiers of log entries written by the commerce components. IDs don't overlap with <see cref="LogEvents"/>.
/// </summary>
public static class CommerceLogEvents
{
    public static readonly EventId OrderCompletedFired = new(41110, nameof(OrderCompletedFired));
    public static readonly EventId OrderCompletedNotFired = new(41111, nameof(OrderCompletedNotFired));
    public static readonly EventId OrderCompletedFailed = new(41112, nameof(OrderCompletedFailed));
}
