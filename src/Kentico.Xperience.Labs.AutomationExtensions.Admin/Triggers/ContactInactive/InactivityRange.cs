namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

/// <summary>
/// Last activity day matched by a <see cref="ContactInactiveTask"/> run, with its server-local time range.
/// </summary>
/// <param name="LastActivityDate">Day of the last activity.</param>
/// <param name="Start">Inclusive start (local midnight of <paramref name="LastActivityDate"/>).</param>
/// <param name="End">Exclusive end (local midnight of the following day).</param>
public readonly record struct InactivityRange(DateOnly LastActivityDate, DateTime Start, DateTime End);
