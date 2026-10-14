using CMS.Automation.Internal;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

/// <summary>
/// Supplies the data property names of all triggers for <see cref="StartedByTriggerProperties.Field"/> (distinct, sorted).
/// Fallback when <see cref="TriggerDataFieldConfigurator"/> doesn't run; the condition rejects properties the selected
/// trigger doesn't have at runtime.
/// </summary>
internal sealed class TriggerDataFieldOptionsProvider(ICustomAutomationTriggerProvider triggerProvider) : IDropDownOptionsProvider
{
    private readonly ICustomAutomationTriggerProvider triggerProvider = triggerProvider;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = RegisteredTriggers.GetAll(triggerProvider)
            .SelectMany(trigger => trigger.DataProperties)
            .Select(property => property.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(name => new DropDownOptionItem { Value = name, Text = name })
            .ToList();

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
