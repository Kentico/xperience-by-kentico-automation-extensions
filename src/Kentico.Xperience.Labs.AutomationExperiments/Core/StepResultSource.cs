namespace Kentico.Xperience.Labs.AutomationExperiments.Core;

/// <summary>
/// Step type that saves <see cref="StepResult"/> entries and appears in the "Previous step succeeded" condition.
/// Register with <c>AddStepResultSource</c>.
/// </summary>
/// <param name="Name">Stable, unique name used in persisted result keys (e.g. <c>Acme.SyncToCrm</c>). Never change once used.</param>
/// <param name="DisplayName">Text shown to marketers – an admin localization resource key or plain text.</param>
public sealed record StepResultSource(string Name, string DisplayName);
