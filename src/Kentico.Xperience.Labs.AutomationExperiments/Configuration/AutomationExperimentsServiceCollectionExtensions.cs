using Kentico.Xperience.Labs.AutomationExperiments.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExperiments.Configuration;
using Kentico.Xperience.Labs.AutomationExperiments.Core;
using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the automation experiments library services.
/// </summary>
public static class AutomationExperimentsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required by the automation experiments components and binds
    /// <see cref="AutomationExperimentsOptions"/> from the <see cref="AutomationExperimentsOptions.SECTION_NAME"/> section.
    /// All components are disabled unless enabled in configuration.
    /// </summary>
    public static IServiceCollection AddAutomationExperiments(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AutomationExperimentsOptions>()
            .Bind(configuration.GetSection(AutomationExperimentsOptions.SECTION_NAME));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAutomationComponentStatus, AutomationComponentStatus>();
        services.AddHostedService<AutomationComponentStatusLogger>();

        services.AddHttpClient(WebhookSender.HTTP_CLIENT_NAME);
        services.TryAddSingleton<IWebhookSender, WebhookSender>();

        services.TryAddSingleton<DateFieldResolver>();

        services.AddStepResultSource(StepResultKeys.SendWebhook);

        return services;
    }
}
