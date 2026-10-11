using Kentico.Xperience.Labs.AutomationExtensions.Actions.AdjustContactScore;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SendWebhook;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the automation extensions library services.
/// </summary>
public static class AutomationExtensionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required by the automation extensions components and binds
    /// <see cref="AutomationExtensionsOptions"/> from the <see cref="AutomationExtensionsOptions.SECTION_NAME"/> section.
    /// All components are disabled unless enabled in configuration.
    /// </summary>
    public static IServiceCollection AddAutomationExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AutomationExtensionsOptions>()
            .Bind(configuration.GetSection(AutomationExtensionsOptions.SECTION_NAME));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAutomationComponentStatus, AutomationComponentStatus>();
        services.AddHostedService<AutomationComponentStatusLogger>();

        services.AddHttpClient(WebhookSender.HTTP_CLIENT_NAME);
        services.TryAddSingleton<IWebhookSender, WebhookSender>();

        services.TryAddSingleton<DateFieldResolver>();
        services.TryAddSingleton<ContactGroupResolver>();
        services.TryAddSingleton<ContactScoreFieldResolver>();
        services.TryAddSingleton<ContactFieldResolver>();
        services.TryAddSingleton<ActivityTypeResolver>();
        services.TryAddSingleton<ActivityLanguageResolver>();
        services.TryAddSingleton<IActivityCounter, ActivityCounter>();

        services.AddStepResultSource(StepResultKeys.SendWebhook);
        services.AddStepResultSource(StepResultKeys.SetContactField);

        return services;
    }
}
