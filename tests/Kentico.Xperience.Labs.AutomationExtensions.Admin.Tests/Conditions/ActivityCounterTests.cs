using CMS.Activities;
using CMS.DataEngine;

using Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;
using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

/// <summary>
/// Query shape of <see cref="ActivityCounter"/> without a database: the settings the DataEngine turns into SQL.
/// </summary>
public class ActivityCounterTests
{
    [Test]
    public void GroupByValue_ProjectsColumnAndCountGroupedOrderedAndLimited()
    {
        var query = ActivityCounter.GroupByValue(new ObjectQuery<ActivityInfo>(), "ActivityURL", 10_001);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo($"[ActivityURL], (COUNT(*)) AS [{ActivityCounter.COUNT_COLUMN}]"));
            Assert.That(query.GroupByColumns, Is.EqualTo("ActivityURL"));
            Assert.That(query.OrderByColumns, Is.EqualTo("ActivityURL"));
            Assert.That(query.TopNRecords, Is.EqualTo(10_001));
        });
    }


    [Test]
    public void Filter_AddsParameterizedPrefilterAndSkipsEmptyUrlConditions()
    {
        UrlMatchRuleSet.TryParse("""{"version":1,"rules":[{"part":"Path","operator":"StartsWith","value":"/pricing"}]}""", out var withLiteral, out _);
        UrlMatchRuleSet.TryParse("""{"version":1,"rules":[{"part":"Path","operator":"Equal","value":"/"}]}""", out var withoutLiteral, out _);

        var query = ActivityCounter.Filter(
            new ObjectQuery<ActivityInfo>(),
            42,
            "pagevisit",
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Unspecified),
            [new ActivityUrlMatchCriterion("ActivityURL", withoutLiteral), new ActivityUrlMatchCriterion("ActivityURL", withLiteral)]);

        string where = query.WhereCondition;
        Assert.Multiple(() =>
        {
            Assert.That(where, Does.Match(@"^\[ActivityContactID\] = @\w+ AND \[ActivityType\] = @\w+ AND \[ActivityCreated\] >= @\w+ AND \[ActivityURL\] LIKE @\w+$"));
            Assert.That(where, Does.Not.Contain("pricing"), "Marketer text must not be part of the SQL text.");
            Assert.That(query.Parameters.Select(parameter => parameter.Value), Does.Contain("%pricing%"));
        });
    }
}
