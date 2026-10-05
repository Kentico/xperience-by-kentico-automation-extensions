using System.Reflection;

using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Labs.AutomationExtensions.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.PreviousStepSucceeded;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Core;

public class SkippableStepTests
{
    [TestCase(false, SkippableStep.BRANCH_YES, false)]
    [TestCase(true, SkippableStep.BRANCH_YES, true)]
    [TestCase(true, SkippableStep.BRANCH_NO, false)]
    [TestCase(true, "unknown", false)]
    public void TryGetSkippedResult_ReturnsSkipStateAndBranch(bool skipStep, string branch, bool expectedResult)
    {
        var properties = new PreviousStepSucceededConditionProperties { SkipStep = skipStep, SkippedBranch = branch };

        bool skipped = SkippableStep.TryGetSkippedResult(properties, out bool result);

        Assert.Multiple(() =>
        {
            Assert.That(skipped, Is.EqualTo(skipStep));
            Assert.That(result, Is.EqualTo(expectedResult));
        });
    }


    [Test]
    public void SkippedBranch_DefaultsToNo() => Assert.That(new PreviousStepSucceededConditionProperties().SkippedBranch, Is.EqualTo(SkippableStep.BRANCH_NO));


    [Test]
    public void CreateSkipped_NotSucceededAndMarkedSkipped()
    {
        var result = StepResult.CreateSkipped("Labs.AutomationExtensions.SendWebhook", DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Skipped, Is.True);
            Assert.That(result.Message, Is.EqualTo(StepResult.SKIPPED_MESSAGE));
        });
    }


    [Test]
    public void StepResultSummary_SkippedResult_ShowsSkipped()
    {
        var data = new StepResultData();
        data.Results["SendWebhook.1"] = StepResult.CreateSkipped("x", new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));

        Assert.That(StepResultSummary.Format(data), Is.EqualTo("SendWebhook.1: skipped (Skipped, 2026-10-04T12:00:00Z)"));
    }


    [Test]
    public void LibraryStepProperties_AllSkippable()
    {
        var propertiesTypes = typeof(AutomationComponentCatalog).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                && (typeof(IAutomationActionProperties).IsAssignableFrom(type) || typeof(IAutomationConditionProperties).IsAssignableFrom(type)))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(propertiesTypes, Is.Not.Empty);
            foreach (var type in propertiesTypes)
            {
                bool skippable = type.IsSubclassOf(typeof(SkippableActionProperties)) || type.IsSubclassOf(typeof(SkippableConditionProperties));
                Assert.That(skippable, Is.True, $"{type.Name} must inherit a skippable base properties class.");
            }
        });
    }


    [Test]
    public void LibraryStepProperties_StepBehaviorCategoryIsLast()
    {
        var propertiesTypes = typeof(AutomationComponentCatalog).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                && (type.IsSubclassOf(typeof(SkippableActionProperties)) || type.IsSubclassOf(typeof(SkippableConditionProperties))));

        foreach (var type in propertiesTypes)
        {
            int maximumOwnOrder = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(property => property.GetCustomAttributes<FormComponentAttribute>())
                .Select(attribute => attribute.Order)
                .DefaultIfEmpty(0)
                .Max();
            int maximumCategoryOrder = type.GetCustomAttributes<FormCategoryAttribute>(inherit: false)
                .Select(attribute => attribute.Order)
                .DefaultIfEmpty(0)
                .Max();

            Assert.That(Math.Max(maximumOwnOrder, maximumCategoryOrder), Is.LessThan(SkippableStep.CATEGORY_ORDER), type.Name);
        }
    }
}
