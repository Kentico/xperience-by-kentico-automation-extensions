using Kentico.Xperience.Labs.AutomationExperiments.Actions.WriteToLog;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Actions;

public class LogMessageFormatterTests
{
    private static readonly Dictionary<string, string> values = new()
    {
        [LogMessageFormatter.PROCESS_NAME] = "Lead nurture",
        [LogMessageFormatter.CONTACT_ID] = "42"
    };


    [Test]
    public void Format_KnownPlaceholders_Replaced()
    {
        string message = LogMessageFormatter.Format("{processName}: contact {contactId} reached step", values);

        Assert.That(message, Is.EqualTo("Lead nurture: contact 42 reached step"));
    }


    [Test]
    public void Format_PlaceholderDifferentCase_Replaced()
    {
        Assert.That(LogMessageFormatter.Format("{PROCESSNAME}", values), Is.EqualTo("Lead nurture"));
    }


    [Test]
    public void Format_UnknownPlaceholder_KeptUnchanged()
    {
        Assert.That(LogMessageFormatter.Format("{contactEmail} {processName}", values), Is.EqualTo("{contactEmail} Lead nurture"));
    }


    [Test]
    public void DefaultMessage_UsesOnlyKnownPlaceholders()
    {
        var allValues = new Dictionary<string, string>
        {
            [LogMessageFormatter.PROCESS_NAME] = "p",
            [LogMessageFormatter.CONTACT_ID] = "1",
            [LogMessageFormatter.CONTACT_GUID] = "g",
            [LogMessageFormatter.TRIGGER_IDENTIFIER] = "t"
        };

        string message = LogMessageFormatter.Format(WriteToLogActionProperties.DEFAULT_MESSAGE, allValues);

        Assert.That(message, Does.Not.Contain("{").And.Not.Contain("}"));
    }


    [Test]
    public void Format_Empty_ReturnsEmpty()
    {
        Assert.That(LogMessageFormatter.Format(string.Empty, values), Is.Empty);
    }
}
