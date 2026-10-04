using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;

/// <summary>
/// Supplies the log levels selectable in <see cref="WriteToLogActionProperties"/>.
/// </summary>
internal sealed class LogLevelOptionsProvider(ILocalizationService localizationService) : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(LogLevel.Information, "labs.automationexperiments.writetolog.level.information"),
            CreateItem(LogLevel.Warning, "labs.automationexperiments.writetolog.level.warning"),
            CreateItem(LogLevel.Error, "labs.automationexperiments.writetolog.level.error")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(LogLevel level, string resourceKey) => new()
    {
        Value = level.ToString(),
        Text = localizationService.GetString(resourceKey)
    };
}
