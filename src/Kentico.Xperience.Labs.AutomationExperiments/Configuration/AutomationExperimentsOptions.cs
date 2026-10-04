using Kentico.Xperience.Labs.AutomationExperiments.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

namespace Kentico.Xperience.Labs.AutomationExperiments.Configuration;

/// <summary>
/// Root options of the library, bound from the <see cref="SECTION_NAME"/> configuration section.
/// </summary>
public sealed class AutomationExperimentsOptions
{
    /// <summary>
    /// Name of the configuration section the options are bound from.
    /// </summary>
    public const string SECTION_NAME = "AutomationExperiments";


    /// <summary>
    /// Enabled state of components not listed in <see cref="Components"/>. Defaults to <c>false</c> (opt-in).
    /// </summary>
    public bool EnabledByDefault { get; set; }


    /// <summary>
    /// Per-component enabled state, keyed by component name (see <see cref="AutomationComponentCatalog"/>).
    /// Overrides <see cref="EnabledByDefault"/>.
    /// </summary>
    public Dictionary<string, bool> Components { get; set; } = new(StringComparer.OrdinalIgnoreCase);


    /// <summary>
    /// Settings of the <see cref="SendWebhookAction"/>.
    /// </summary>
    public WebhookOptions Webhooks { get; set; } = new();


    /// <summary>
    /// Settings of the <see cref="ContactDateAnniversaryTrigger"/>.
    /// </summary>
    public ContactDateAnniversaryOptions ContactDateAnniversary { get; set; } = new();
}
