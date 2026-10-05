namespace Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;

/// <summary>
/// Contact field available to the <see cref="AdjustContactScoreAction"/>.
/// </summary>
/// <param name="Name">Field (column) name.</param>
/// <param name="Caption">Localized field caption.</param>
public sealed record ContactScoreField(string Name, string Caption);
