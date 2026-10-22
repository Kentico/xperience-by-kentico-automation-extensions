using System.Reflection;

using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.RecipientListMember;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

/// <summary>
/// Executing the member, confirmation and contact queries requires an Xperience database, so the firing path is covered by
/// the query shapes (members: <see cref="ContactGroupMemberTask.SelectContactIds"/>, tested for the contact group trigger;
/// subscribed contacts: <see cref="RecipientListMemberTask.SelectApprovedContactIds"/>; bounced contacts:
/// <see cref="RecipientListMemberTask.SelectBouncedEmailAddresses"/>) and running-app validation.
/// </summary>
public class RecipientListMemberTaskTests
{
    [Test]
    public async Task Execute_Disabled_ReturnsResultTextWithoutQueries()
    {
        var options = new AutomationExtensionsOptions { RecipientListMember = { RecipientLists = ["Newsletter"] } };

        string? message = await Execute(options);

        Assert.That(message,
            Is.EqualTo($"Disabled via configuration ('{AutomationComponentCatalog.RecipientListMember.ConfigurationPath}'). No triggers fired."));
    }


    [Test]
    public async Task Execute_NoListsConfigured_ReturnsResultTextWithoutQueries()
    {
        var options = new AutomationExtensionsOptions
        {
            Components = { ["RecipientListMember"] = true },
            RecipientListMember = { RecipientLists = [" ", ""] }
        };

        string? message = await Execute(options);

        Assert.That(message,
            Is.EqualTo("No recipient lists configured in 'AutomationExtensions:RecipientListMember:RecipientLists'. No triggers fired."));
    }


    [Test]
    public async Task Execute_NoConfiguredListResolves_ReturnsResultTextWithoutMemberQueries()
    {
        var options = new AutomationExtensionsOptions
        {
            Components = { ["RecipientListMember"] = true },
            RecipientListMember = { RecipientLists = ["Unknown", "Customers"] }
        };

        string? message = await Execute(options, [new(2, "Customers", "Customers", ContactGroupCandidateKind.ContactGroup)]);

        Assert.That(message, Does.StartWith("None of the recipient lists configured in 'AutomationExtensions:RecipientListMember:RecipientLists' exist"));
    }


    [Test]
    public void SelectApprovedContactIds_UsesLatestConfirmationPerContactAndRequiresApproval()
    {
        // A custom query text over the confirmation table lets the nested query render without an Xperience database.
        var confirmations = new DataQuery { CustomQueryText = "SELECT ##TOPN## ##COLUMNS## FROM [EmailLibrary_EmailSubscriptionConfirmation] WHERE ##WHERE## ORDER BY ##ORDERBY##" };

        var query = RecipientListMemberTask.SelectApprovedContactIds(confirmations, 7);
        string sql = query.ToString(false);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[EmailSubscriptionConfirmationContactID]"));
            Assert.That(sql, Does.Contain("FROM [EmailLibrary_EmailSubscriptionConfirmation]")
                .And.Contain("ROW_NUMBER() OVER (PARTITION BY EmailSubscriptionConfirmationContactID ORDER BY EmailSubscriptionConfirmationDate DESC, EmailSubscriptionConfirmationID DESC)")
                .And.Contain("[EmailSubscriptionConfirmationRecipientListID] = @")
                .And.Contain("[LabsConfirmationRowNumber] = @")
                .And.Contain("[EmailSubscriptionConfirmationIsApproved] = @"));
            Assert.That(query.Parameters.Select(parameter => parameter.Value), Is.SupersetOf(new object[] { 7, 1, true }));
        });
    }


    [TestCase(5)]
    [TestCase(0)]
    public void SelectBouncedEmailAddresses_HardBounceOrSoftBounceCountAtLimit(int softBounceLimit)
    {
        // A custom query text over the bounce table lets the query render without an Xperience database.
        var bounces = new DataQuery { CustomQueryText = "SELECT ##COLUMNS## FROM [EmailLibrary_EmailBounce] WHERE ##WHERE##" };

        var query = RecipientListMemberTask.SelectBouncedEmailAddresses(bounces, softBounceLimit);
        string sql = query.ToString(false);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[EmailBounceEmailAddress]"));
            Assert.That(sql, Does.Contain("([EmailBounceIsHardBounce] = @EmailBounceIsHardBounce OR [EmailBounceSoftBounceCount] >= @EmailBounceSoftBounceCount)"));
            Assert.That(query.Parameters.Select(parameter => parameter.Value), Is.SupersetOf(new object[] { true, softBounceLimit }));
        });
    }


    private static async Task<string?> Execute(AutomationExtensionsOptions options, IEnumerable<ContactGroupCandidate>? candidates = null)
    {
        var monitor = new TestOptionsMonitor<AutomationExtensionsOptions>(options);
        var resolver = new RecipientListResolver(monitor, (_, _) => candidates is null
            ? throw new AssertionException("Candidates must not be loaded.")
            : Task.FromResult(candidates));
        // Null dispatcher, providers and bounce options: any member query or fired trigger would throw.
        var task = new RecipientListMemberTask(null!, null!, null!, null!, null!, null!, resolver, new AutomationComponentStatus(monitor), monitor, NullLoggerFactory.Instance);

        var result = await task.Execute(new ScheduledTaskConfigurationInfo(), CancellationToken.None);

        // ScheduledTaskExecutionResult.Result isn't public.
        return (string?)typeof(ScheduledTaskExecutionResult)
            .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(result);
    }
}
