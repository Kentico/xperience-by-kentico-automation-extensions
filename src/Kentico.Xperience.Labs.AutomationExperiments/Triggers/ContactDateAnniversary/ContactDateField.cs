namespace Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

/// <summary>
/// Contact date field available to the <see cref="ContactDateAnniversaryTrigger"/>.
/// </summary>
/// <param name="Name">Field (column) name.</param>
/// <param name="Caption">Localized field caption.</param>
public sealed record ContactDateField(string Name, string Caption);
