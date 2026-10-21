using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<RandomSplitCondition>(
    RandomSplitCondition.IDENTIFIER,
    "labs.automationextensions.randomsplit.displayname",
    Description = "labs.automationextensions.randomsplit.description",
    IconName = Icons.Cogwheel)]

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;

/// <summary>
/// Assigns a processed contact to a stable Yes/No branch using its GUID and the configured split key.
/// </summary>
public sealed class RandomSplitCondition(
    IAutomationComponentStatus componentStatus,
    ILoggerFactory loggerFactory) : AutomationCondition<RandomSplitConditionProperties>
{
    public const string NAME = "RandomSplit";
    public const string IDENTIFIER = AutomationComponentCatalog.IDENTIFIER_PREFIX + NAME;


    private readonly IAutomationComponentStatus componentStatus = componentStatus;
    private readonly ILogger logger = loggerFactory.CreateLogger(LogCategories.For(AutomationComponentCatalog.RandomSplit));


    /// <inheritdoc />
    public override async Task<bool> Evaluate(
        RandomSplitConditionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (!componentStatus.IsEnabled(AutomationComponentCatalog.RandomSplit))
        {
            DisabledComponentLog.Write(logger, AutomationComponentCatalog.RandomSplit, context.Process.DisplayName, "sends all contacts through the No branch");
            return false;
        }

        if (SkippableStep.TryGetSkippedResult(properties, out bool skippedResult))
        {
            SkippableStep.LogSkipped(logger, IDENTIFIER, context.Process.DisplayName);
            return skippedResult;
        }

        RandomSplitBucket.ValidateConfiguration(properties.Percentage, properties.SplitKey);
        cancellationToken.ThrowIfCancellationRequested();

        var contact = await context.GetProcessedObject(cancellationToken);

        return RandomSplitBucket.IsInYesBranch(contact.ContactGUID, properties.SplitKey, properties.Percentage);
    }
}
