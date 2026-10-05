using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.Filters;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Admin;

public sealed class StepRegistryFilterModel
{
    [DropDownComponent(
        Label = "{$labs.automationextensions.stepregistry.kind$}",
        Placeholder = "{$labs.automationextensions.stepregistry.all$}",
        Options = "trigger;{$labs.automationextensions.stepregistry.trigger$}\r\ncondition;{$labs.automationextensions.stepregistry.condition$}\r\naction;{$labs.automationextensions.stepregistry.action$}\r\nunknown;{$labs.automationextensions.stepregistry.unknown$}",
        Order = 0)]
    [FilterCondition(BuilderType = typeof(StepRegistryFilterConditionBuilder))]
    public string? Kind { get; set; }


    [DropDownComponent(
        Label = "{$labs.automationextensions.stepregistry.status$}",
        Placeholder = "{$labs.automationextensions.stepregistry.all$}",
        Options = "enabled;{$labs.automationextensions.stepregistry.enabled$}\r\ndisabled;{$labs.automationextensions.stepregistry.disabled$}\r\nnotmanaged;{$labs.automationextensions.stepregistry.notmanaged$}\r\nnotapplicable;{$labs.automationextensions.stepregistry.notapplicable$}",
        Order = 1)]
    [FilterCondition(BuilderType = typeof(StepRegistryFilterConditionBuilder))]
    public string? Status { get; set; }
}

public sealed class StepRegistryFilterConditionBuilder : IWhereConditionBuilder
{
    // DataContainerListingPage ignores SQL conditions. The page filters the bound model
    // in memory before the built-in search, sort, count, and paging run.
    public Task<IWhereCondition> Build(string columnName, object value) =>
        Task.FromResult<IWhereCondition>(new WhereCondition());
}
