using Kentico.Xperience.Labs.AutomationExtensions.Core;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Core;

public class StepResultRecorderTests
{
    private static readonly string webhookResult1 = StepResultKeys.Create(StepResultKeys.SendWebhook.Name, "1")!;
    private static readonly string webhookResult2 = StepResultKeys.Create(StepResultKeys.SendWebhook.Name, "2")!;


    [Test]
    public void Find_NoData_ReturnsNull()
    {
        Assert.That(StepResultRecorder.Find(null, webhookResult1), Is.Null);
    }


    [Test]
    public void Find_KeyNotRecorded_ReturnsNull()
    {
        var data = CreateData(webhookResult2, true);

        Assert.That(StepResultRecorder.Find(data, webhookResult1), Is.Null);
    }


    [Test]
    public void Find_KeyRecorded_ReturnsResult()
    {
        var data = CreateData(webhookResult1, true);

        Assert.That(StepResultRecorder.Find(data, webhookResult1.ToLowerInvariant())?.Succeeded, Is.True);
    }


    [Test]
    public void Find_EmptyKey_ReturnsNull()
    {
        var data = CreateData(webhookResult1, true);

        Assert.That(StepResultRecorder.Find(data, string.Empty), Is.Null);
    }


    private static StepResultData CreateData(string key, bool succeeded)
    {
        var data = new StepResultData();
        data.Results[key] = new StepResult { Succeeded = succeeded };

        return data;
    }
}
