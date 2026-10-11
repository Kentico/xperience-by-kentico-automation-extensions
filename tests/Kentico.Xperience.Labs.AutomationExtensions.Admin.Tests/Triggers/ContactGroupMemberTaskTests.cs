using System.Reflection;

using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.Scheduler;

using Kentico.Xperience.Labs.AutomationExtensions.Configuration;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

/// <summary>
/// Executing the member and contact queries requires an Xperience database, so the firing path is covered by the query
/// shape (<see cref="ContactGroupMemberTask.SelectContactIds"/>) and running-app validation.
/// </summary>
public class ContactGroupMemberTaskTests
{
    [Test]
    public async Task Execute_Disabled_ReturnsResultTextWithoutQueries()
    {
        var options = new AutomationExtensionsOptions { ContactGroupMember = { ContactGroups = ["Customers"] } };

        string? message = await Execute(options);

        Assert.That(message,
            Is.EqualTo($"Disabled via configuration ('{AutomationComponentCatalog.ContactGroupMember.ConfigurationPath}'). No triggers fired."));
    }


    [Test]
    public async Task Execute_NoGroupsConfigured_ReturnsResultTextWithoutQueries()
    {
        var options = new AutomationExtensionsOptions
        {
            Components = { ["ContactGroupMember"] = true },
            ContactGroupMember = { ContactGroups = [" ", ""] }
        };

        string? message = await Execute(options);

        Assert.That(message,
            Is.EqualTo("No contact groups configured in 'AutomationExtensions:ContactGroupMember:ContactGroups'. No triggers fired."));
    }


    [Test]
    public async Task Execute_NoConfiguredGroupResolves_ReturnsResultTextWithoutMemberQueries()
    {
        var options = new AutomationExtensionsOptions
        {
            Components = { ["ContactGroupMember"] = true },
            ContactGroupMember = { ContactGroups = ["Unknown", "Newsletter"] }
        };

        string? message = await Execute(options, [new(2, "Newsletter", "Newsletter", ContactGroupCandidateKind.RecipientList)]);

        Assert.That(message, Does.StartWith("None of the contact groups configured in 'AutomationExtensions:ContactGroupMember:ContactGroups' exist"));
    }


    [Test]
    public void SelectContactIds_FiltersGroupAndContactMembersWithParameterizedKeysetPaging()
    {
        var query = ContactGroupMemberTask.SelectContactIds(new DataQuery(), 7, 1234, 500);

        Assert.Multiple(() =>
        {
            Assert.That(query.SelectColumnsList.ToString(), Is.EqualTo("[ContactGroupMemberRelatedID]"));
            Assert.That(query.WhereCondition, Does.Contain("[ContactGroupMemberContactGroupID] = @")
                .And.Contain("[ContactGroupMemberType] = @")
                .And.Contain("[ContactGroupMemberRelatedID] > @"));
            Assert.That(query.OrderByColumns, Is.EqualTo("ContactGroupMemberRelatedID"));
            Assert.That(query.TopNRecords, Is.EqualTo(500));
            Assert.That(query.Parameters.Select(parameter => parameter.Value),
                Is.SupersetOf(new object[] { 7, (int)ContactGroupMemberTypeEnum.Contact, 1234 }));
        });
    }


    [Test]
    public void ContactMemberType_IsZero() => Assert.That((int)ContactGroupMemberTypeEnum.Contact, Is.Zero);


    private static async Task<string?> Execute(AutomationExtensionsOptions options, IEnumerable<ContactGroupCandidate>? candidates = null)
    {
        var monitor = new TestOptionsMonitor<AutomationExtensionsOptions>(options);
        var resolver = new ContactGroupResolver(monitor, (_, _) => candidates is null
            ? throw new AssertionException("Candidates must not be loaded.")
            : Task.FromResult(candidates));
        // Null dispatcher and providers: any member query or fired trigger would throw.
        var task = new ContactGroupMemberTask(null!, null!, null!, resolver, new AutomationComponentStatus(monitor), monitor, NullLoggerFactory.Instance);

        var result = await task.Execute(new ScheduledTaskConfigurationInfo(), CancellationToken.None);

        // ScheduledTaskExecutionResult.Result isn't public.
        return (string?)typeof(ScheduledTaskExecutionResult)
            .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetValue(result);
    }
}
