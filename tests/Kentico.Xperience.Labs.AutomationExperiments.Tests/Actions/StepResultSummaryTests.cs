using Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;
using Kentico.Xperience.Labs.AutomationExperiments.Core;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Actions;

public class StepResultSummaryTests
{
    [Test]
    public void Format_NoData_ReturnsNone()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StepResultSummary.Format(null), Is.EqualTo(StepResultSummary.NO_RESULTS));
            Assert.That(StepResultSummary.Format(new StepResultData()), Is.EqualTo(StepResultSummary.NO_RESULTS));
        });
    }


    [Test]
    public void Format_Results_OrderedByKeyWithOutcome()
    {
        var recordedAt = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
        var data = new StepResultData();
        data.Results["SendWebhook.2"] = new StepResult { Succeeded = false, Message = "Timed out", RecordedAt = recordedAt };
        data.Results["SendWebhook.1"] = new StepResult { Succeeded = true, Message = "HTTP 202", RecordedAt = recordedAt };

        string summary = StepResultSummary.Format(data);

        Assert.That(summary, Is.EqualTo(
            "SendWebhook.1: succeeded (HTTP 202, 2026-10-04T12:00:00Z); SendWebhook.2: failed (Timed out, 2026-10-04T12:00:00Z)"));
    }
}
