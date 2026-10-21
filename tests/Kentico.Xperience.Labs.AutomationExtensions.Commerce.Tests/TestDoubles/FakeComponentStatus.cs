using Kentico.Xperience.Labs.AutomationExtensions.Configuration;

namespace Kentico.Xperience.Labs.AutomationExtensions.Commerce.Tests.TestDoubles;

internal sealed class FakeComponentStatus(bool enabled) : IAutomationComponentStatus
{
    public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
}
