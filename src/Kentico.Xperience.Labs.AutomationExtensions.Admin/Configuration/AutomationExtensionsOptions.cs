using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Configuration;

/// <summary>
/// Root options of the library, bound from the <see cref="SECTION_NAME"/> configuration section.
/// </summary>
public sealed class AutomationExtensionsOptions
{
    /// <summary>
    /// Name of the configuration section the options are bound from.
    /// </summary>
    public const string SECTION_NAME = "AutomationExtensions";


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
    /// Settings of the <see cref="PostChatMessageAction"/>.
    /// </summary>
    public ChatChannelOptions ChatChannels { get; set; } = new();


    /// <summary>
    /// Settings of the <see cref="ContactDateAnniversaryTrigger"/>.
    /// </summary>
    public ContactDateAnniversaryOptions ContactDateAnniversary { get; set; } = new();


    /// <summary>
    /// Settings of the <see cref="ContactInactiveTrigger"/>.
    /// </summary>
    public ContactInactiveOptions ContactInactive { get; set; } = new();


    /// <summary>
    /// Settings of the <see cref="ContactGroupMemberTrigger"/>.
    /// </summary>
    public ContactGroupMemberOptions ContactGroupMember { get; set; } = new();
}
