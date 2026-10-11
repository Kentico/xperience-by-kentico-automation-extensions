using CMS.Automation.Internal;

using Kentico.Xperience.Admin.Base.Forms;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Limits the <see cref="StartedByTriggerProperties.Field"/> options to the data properties of the trigger selected in
/// <see cref="StartedByTriggerProperties.Trigger"/>, and clears the field when the trigger changes.
/// </summary>
/// <remarks>
/// Runs on dialog load and whenever the trigger changes (documented <c>FormComponentConfiguration</c> dependency).
/// Triggers without data, unregistered triggers, and no selection get no options.
/// </remarks>
internal sealed class TriggerDataFieldConfigurator(ICustomAutomationTriggerProvider triggerProvider) : FormComponentConfigurator<DropDownComponent>
{
    /// <summary>
    /// Separator between value and text in <see cref="DropDownProperties.Options"/>.
    /// </summary>
    internal const string OPTIONS_SEPARATOR = ";";


    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;


    /// <inheritdoc />
    public override Task Configure(DropDownComponent formComponent, IFormFieldValueProvider formFieldValueProvider, CancellationToken cancellationToken)
    {
        formFieldValueProvider.TryGet(nameof(StartedByTriggerProperties.Trigger), out string? identifier);

        formComponent.Properties.DataProviderType = null;
        formComponent.Properties.OptionsValueSeparator = OPTIONS_SEPARATOR;
        formComponent.Properties.Options = GetOptions(RegisteredTriggers.Find(triggerProvider, identifier));

        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public override Task<object> ConfigureValue(IFormFieldValueProvider formFieldValueProvider, string changedFieldName, CancellationToken cancellationToken)
    {
        // A property of the previous trigger would fail validation (and the runtime check), so start over.
        if (string.Equals(changedFieldName, nameof(StartedByTriggerProperties.Trigger), StringComparison.Ordinal))
        {
            return Task.FromResult<object>(string.Empty);
        }

        return base.ConfigureValue(formFieldValueProvider, changedFieldName, cancellationToken);
    }


    /// <summary>
    /// Returns the options of the given trigger in the <see cref="DropDownProperties.Options"/> format (one
    /// <c>value;text</c> pair per line; value and text = property name). Empty for <c>null</c> or triggers without data.
    /// </summary>
    internal static string GetOptions(RegisteredTrigger? trigger) => trigger is null
        ? string.Empty
        : string.Join("\r\n", trigger.DataProperties.Select(property => property.Name + OPTIONS_SEPARATOR + property.Name));
}
