namespace Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;

/// <summary>
/// Contact date field available to the <see cref="ContactDateAnniversaryTrigger"/>.
/// </summary>
/// <param name="Name">Field (column) name.</param>
/// <param name="Caption">Localized field caption.</param>
public sealed record ContactDateField(string Name, string Caption);
