using System.Reflection;

using CMS.Automation;
using CMS.Automation.Internal;
using CMS.ContactManagement;

using Kentico.Xperience.Labs.AutomationExtensions.Actions.PostChatMessage;
using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Core;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class PostChatMessageActionTests
{
    private static readonly DateTimeOffset now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);


    [TestCase(false)]
    [TestCase(true)]
    public async Task Execute_DisabledComponentDoesNothing(bool skipStep)
    {
        var sender = new RecordingSender(new ChatSendResult(true, "HTTP 200"));
        var processData = new ProcessDataStore();
        var properties = new PostChatMessageActionProperties { SkipStep = skipStep, ChannelName = "sales", Message = "x", RecordResult = true };

        await CreateAction(enabled: false, sender).Execute(properties, CreateContext(processData), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sender.Messages, Is.Empty);
            Assert.That(processData.Value, Is.Null);
        });
    }


    [Test]
    public async Task Execute_SkippedStepRecordsSkippedResultWithoutPosting()
    {
        var sender = new RecordingSender(new ChatSendResult(true, "HTTP 200"));
        var context = CreateContext(new ProcessDataStore());
        var properties = new PostChatMessageActionProperties { SkipStep = true, ChannelName = "sales", Message = "x", RecordResult = true, ResultNumber = "2" };

        await CreateAction(enabled: true, sender).Execute(properties, context, CancellationToken.None);

        var result = await GetResult(context, "2");
        Assert.Multiple(() =>
        {
            Assert.That(sender.Messages, Is.Empty);
            Assert.That(result?.Skipped, Is.True);
            Assert.That(result?.ActionIdentifier, Is.EqualTo(PostChatMessageAction.IDENTIFIER));
            Assert.That(result?.RecordedAt, Is.EqualTo(now.UtcDateTime));
        });
    }


    [Test]
    public void Properties_Skippable() =>
        Assert.That(typeof(SkippableActionProperties).IsAssignableFrom(typeof(PostChatMessageActionProperties)), Is.True);


    // Steps past the skip check read ContactInfo properties, which need an initialized Xperience container – the posting path is
    // covered by ChatMessageFactoryTests and ChatMessageSenderTests.
    private static PostChatMessageAction CreateAction(bool enabled, IChatMessageSender sender) => new(
        sender,
        new NoProcessResolver(),
        new StubComponentStatus(enabled),
        ContactFieldResolverTests.CreateResolver(),
        StubTriggerProvider.WithLibraryTriggers(),
        new TestOptionsMonitor<AutomationExtensionsOptions>(new()),
        new FixedTimeProvider(),
        NullLoggerFactory.Instance);


    private static async Task<StepResult?> GetResult(AutomationProcessContext context, string resultNumber) =>
        StepResultRecorder.Find(
            await context.GetProcessData<StepResultData>(),
            StepResultKeys.Create(StepResultKeys.PostChatMessage.Name, resultNumber));


    private static AutomationProcessContext CreateContext(ProcessDataStore processData)
    {
        var constructor = typeof(AutomationProcessContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single();

        // The platform exposes no public test factory for its process context.
        object?[] arguments =
        [
            new ContactInfo(),
            new AutomationProcessMetadata { DisplayName = "Test process" },
            (Func<string>)(() => processData.Value!),
            (Action<string>)(value => processData.Value = value),
            "state",
            new NoTriggerDataSerializer(),
            NullLogger<AutomationProcessContext>.Instance
        ];

        return (AutomationProcessContext)constructor.Invoke(arguments);
    }


    private sealed class NoTriggerDataSerializer : IAutomationTriggerDataSerializer
    {
        public string Serialize(string triggerIdentifier, IAutomationTriggerData triggerData) => throw new NotSupportedException();


        public string GetTriggerIdentifier(string json) => string.Empty;


        public IAutomationTriggerData TryDeserialize(string json) => null!;
    }


    private sealed class ProcessDataStore
    {
        public string? Value { get; set; }
    }


    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }


    private sealed class StubComponentStatus(bool enabled) : IAutomationComponentStatus
    {
        public bool IsEnabled(AutomationComponentDescriptor component) => enabled;
    }


    private sealed class NoProcessResolver : IAutomationProcessResolver
    {
        public Task<int?> FindIdByDisplayName(string? displayName, CancellationToken cancellationToken) => Task.FromResult<int?>(null);
    }


    private sealed class RecordingSender(ChatSendResult result) : IChatMessageSender
    {
        public List<ChatMessage> Messages { get; } = [];


        public Task<ChatSendResult> Send(string channelName, ChatMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);

            return Task.FromResult(result);
        }
    }
}
