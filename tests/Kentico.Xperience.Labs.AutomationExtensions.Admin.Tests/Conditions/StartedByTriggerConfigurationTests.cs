using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

using CMS.Automation;
using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;
using Kentico.Xperience.Labs.AutomationExtensions.Resources;
using Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactDateAnniversary;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactGroupMember;
using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class StartedByTriggerConfigurationTests
{
    private static readonly StubTriggerProvider triggers = StubTriggerProvider.WithLibraryTriggers();


    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION)]
    [TestCase("Acme.UnregisteredTrigger")]
    public void GetConfigurationError_TriggerOnlyIsValidEvenWhenUnregistered(string identifier)
    {
        string? error = StartedByTriggerConfiguration.GetConfigurationError(
            new StartedByTriggerProperties { Trigger = identifier, Field = "Missing", Operator = "Unknown" },
            Find(identifier),
            out var configuration);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(configuration!.TriggerIdentifier, Is.EqualTo(identifier));
            Assert.That(configuration.Property, Is.Null);
        });
    }


    [Test]
    public void GetConfigurationError_DataCheckIsValid()
    {
        string? error = StartedByTriggerConfiguration.GetConfigurationError(
            DataCheck(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, "ActivityType", nameof(TriggerDataOperator.StartsWith), "acme_"),
            Find(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY),
            out var configuration);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(configuration!.Property!.Name, Is.EqualTo("ActivityType"));
            Assert.That(configuration.DataType, Is.EqualTo(typeof(CustomActivityTriggerData)));
            Assert.That(configuration.Operator, Is.EqualTo(TriggerDataOperator.StartsWith));
        });
    }


    [Test]
    public void GetConfigurationError_RegisteredLibraryTriggerDataCheckIsValid()
    {
        string? error = StartedByTriggerConfiguration.GetConfigurationError(
            DataCheck(ContactInactiveTrigger.IDENTIFIER, "InactiveDays", nameof(TriggerDataOperator.GreaterThanOrEqual), "90"),
            Find(ContactInactiveTrigger.IDENTIFIER),
            out var configuration);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Null);
            Assert.That(configuration!.Kind, Is.EqualTo(TriggerDataValueKind.Number));
        });
    }


    [TestCase("", "", "Equal", "", "no trigger is selected")]
    [TestCase("Acme.UnregisteredTrigger", "BizFormId", "Equal", "1", "isn't registered")]
    [TestCase(StubTriggerProvider.NO_DATA_TRIGGER, "BizFormId", "Equal", "1", "provides no trigger data")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "", "Equal", "1", "no trigger data field")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "Identifier", "Equal", "1", "isn't a data property")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "bizformid", "Equal", "1", "isn't a data property")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "MemberId", "Equal", "1", "isn't a data property")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "BizFormId", "Between", "1", "operator 'Between' is unknown")]
    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "BizFormId", "Equal", "one", "isn't a number")]
    [TestCase(ContactInactiveTrigger.IDENTIFIER, "LastActivityDate", "LessThan", "10/11/2026", "isn't a date")]
    [TestCase(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, "ActivityType", "GreaterThan", "a", "can only compare numbers and dates")]
    [TestCase(DefaultTriggerIdentifiers.CUSTOM_ACTIVITY, "ActivityValue", "Contains", "", "no value is entered")]
    public void GetConfigurationError_InvalidSettings(string trigger, string field, string comparisonOperator, string value, string expected)
    {
        string? error = StartedByTriggerConfiguration.GetConfigurationError(DataCheck(trigger, field, comparisonOperator, value), Find(trigger), out var configuration);

        Assert.Multiple(() =>
        {
            Assert.That(error, Does.Contain(expected));
            Assert.That(configuration, Is.Null);
        });
    }


    [Test]
    public void GetConfigurationError_DoesNotContainEnteredValues()
    {
        string? error = StartedByTriggerConfiguration.GetConfigurationError(
            DataCheck(DefaultTriggerIdentifiers.FORM_SUBMISSION, "BizFormId", nameof(TriggerDataOperator.Equal), "jane@example.com"),
            Find(DefaultTriggerIdentifiers.FORM_SUBMISSION),
            out _);

        Assert.That(error, Does.Not.Contain("jane"));
    }


    [TestCase("Kentico.FormSubmissionTrigger", true)]
    [TestCase("kentico.formsubmissiontrigger", false)]
    [TestCase(" Kentico.FormSubmissionTrigger", false)]
    [TestCase("Kentico.RegistrationTrigger", false)]
    [TestCase(null, false)]
    public void Matches_ExactIdentifier(string? identifier, bool expected)
    {
        StartedByTriggerConfiguration.GetConfigurationError(
            new StartedByTriggerProperties { Trigger = DefaultTriggerIdentifiers.FORM_SUBMISSION }, null, out var configuration);

        Assert.That(configuration!.Matches(identifier), Is.EqualTo(expected));
    }


    [Test]
    public void RegisteredTriggers_BuiltInsFirstThenRegisteredTriggers() => Assert.Multiple(() =>
    {
        var all = RegisteredTriggers.GetAll(triggers);

        Assert.That(all.Select(trigger => trigger.Identifier), Is.EqualTo(new[]
        {
            DefaultTriggerIdentifiers.FORM_SUBMISSION,
            DefaultTriggerIdentifiers.REGISTRATION,
            DefaultTriggerIdentifiers.CUSTOM_ACTIVITY,
            "Labs.AutomationExtensions.ContactDateAnniversary",
            "Labs.AutomationExtensions.ContactInactive",
            "Labs.AutomationExtensions.ContactGroupMember",
            StubTriggerProvider.NO_DATA_TRIGGER
        }));
        Assert.That(all.Select(trigger => trigger.DataType), Is.EqualTo(new[]
        {
            typeof(FormSubmissionTriggerData),
            typeof(RegistrationTriggerData),
            typeof(CustomActivityTriggerData),
            typeof(ContactDateAnniversaryTriggerData),
            typeof(ContactInactiveTriggerData),
            typeof(ContactGroupMemberTriggerData),
            null
        }));
    });


    [Test]
    public void RegisteredTriggers_BuiltInIdentifierInStoreIsListedOnce()
    {
        var duplicate = StubTriggerProvider.CreateDefinition(DefaultTriggerIdentifiers.FORM_SUBMISSION, "Duplicate", typeof(StubTriggerProvider.NoDataTrigger));

        var all = RegisteredTriggers.GetAll(new StubTriggerProvider(duplicate));

        Assert.That(all.Single(trigger => trigger.Identifier == DefaultTriggerIdentifiers.FORM_SUBMISSION).DataType, Is.EqualTo(typeof(FormSubmissionTriggerData)));
    }


    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, true)]
    [TestCase("kentico.formsubmissiontrigger", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void RegisteredTriggers_FindIsExact(string? identifier, bool found) =>
        Assert.That(RegisteredTriggers.Find(triggers, identifier), found ? Is.Not.Null : Is.Null);


    [Test]
    public async Task TriggerOptions_BuiltInAndRegisteredTriggersByIdentifierSortedByName()
    {
        var items = (await new TriggerOptionsProvider(triggers, new ResourceLocalization()).GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[]
            {
                "Acme trigger without data", "Contact date anniversary", "Contact in contact group (daily)", "Contact inactive for N days",
                "Custom activity", "Form submission", "Member registration"
            }));
            Assert.That(items.Single(item => item.Text == "Form submission").Value, Is.EqualTo(DefaultTriggerIdentifiers.FORM_SUBMISSION));
            Assert.That(items.Single(item => item.Text == "Contact inactive for N days").Value, Is.EqualTo(ContactInactiveTrigger.IDENTIFIER));
        });
    }


    [Test]
    public async Task FieldOptions_DistinctPropertyNamesOfAllTriggersWithoutIdentifier()
    {
        var items = (await new TriggerDataFieldOptionsProvider(triggers).GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.Unique.And.Ordered.Using((IComparer<string>)StringComparer.Ordinal));
            Assert.That(items.Select(item => item.Value), Has.None.EqualTo("Identifier"));
            Assert.That(items.Select(item => item.Value), Is.EquivalentTo(new[]
            {
                "BizFormId", "BizFormItemId", "MemberId", "ActivityType", "ActivityItemId", "ActivityItemDetailId", "ActivityValue",
                "ActivityChannelId", "FieldName", "DaysBefore", "AnniversaryDate", "Years", "InactiveDays", "LastActivityDate",
                "ContactGroupName", "ContactGroupDisplayName"
            }));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(items.Select(item => item.Value)));
        });
    }


    [Test]
    public async Task OperatorOptions_ListAllOperatorsByNameWithLocalizedTexts()
    {
        var items = (await new TriggerDataOperatorOptionsProvider(new ResourceLocalization()).GetOptionItems()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Value), Is.EqualTo(Enum.GetNames<TriggerDataOperator>()));
            Assert.That(items.Select(item => item.Text), Is.EqualTo(new[]
            {
                "is equal to", "is not equal to", "contains", "does not contain", "starts with", "ends with", "is greater than",
                "is greater than or equal to", "is less than", "is less than or equal to", "is empty", "is not empty"
            }));
        });
    }


    [Test]
    public void OperatorNames_NeverChange() => Assert.That(Enum.GetNames<TriggerDataOperator>(), Is.EqualTo(new[]
    {
        "Equal", "NotEqual", "Contains", "DoesNotContain", "StartsWith", "EndsWith", "GreaterThan", "GreaterThanOrEqual", "LessThan",
        "LessThanOrEqual", "IsEmpty", "IsNotEmpty"
    }));


    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, "BizFormItemId;BizFormItemId\r\nBizFormId;BizFormId")]
    [TestCase(DefaultTriggerIdentifiers.REGISTRATION, "MemberId;MemberId")]
    [TestCase(ContactInactiveTrigger.IDENTIFIER, "InactiveDays;InactiveDays\r\nLastActivityDate;LastActivityDate")]
    [TestCase(StubTriggerProvider.NO_DATA_TRIGGER, "")]
    [TestCase("Acme.UnregisteredTrigger", "")]
    [TestCase(null, "")]
    public async Task FieldConfigurator_ConfiguresDropDownFromSelectedTrigger(string? trigger, string expectedOptions)
    {
        var component = CreateDropDown();
        component.Properties.DataProviderType = typeof(TriggerDataFieldOptionsProvider);

        await new TriggerDataFieldConfigurator(triggers).Configure(component, new FieldValues(trigger), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(component.Properties.DataProviderType, Is.Null);
            Assert.That(component.Properties.OptionsValueSeparator, Is.EqualTo(";"));
            Assert.That(component.Properties.Options, Is.EqualTo(expectedOptions));
        });
    }


    [Test]
    public async Task FieldConfigurator_ClearsFieldOnlyWhenTriggerChanges()
    {
        var configurator = new TriggerDataFieldConfigurator(triggers);

        object onTriggerChange = await configurator.ConfigureValue(new FieldValues(DefaultTriggerIdentifiers.REGISTRATION), nameof(StartedByTriggerProperties.Trigger), CancellationToken.None);
        object onLoad = await configurator.ConfigureValue(new FieldValues(DefaultTriggerIdentifiers.REGISTRATION), null!, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(onTriggerChange, Is.EqualTo(string.Empty));
            Assert.That(onLoad, Is.Not.EqualTo(string.Empty).And.Not.Null);
        });
    }


    [TestCase(DefaultTriggerIdentifiers.FORM_SUBMISSION, true)]
    [TestCase(ContactInactiveTrigger.IDENTIFIER, true)]
    [TestCase(StubTriggerProvider.NO_DATA_TRIGGER, false)]
    [TestCase("Acme.UnregisteredTrigger", false)]
    [TestCase(null, false)]
    public void TriggerHasDataVisibility(string? trigger, bool expected)
    {
        var condition = new TriggerHasDataVisibilityCondition(triggers);
        condition.Properties.PropertyName = nameof(StartedByTriggerProperties.Trigger);

        Assert.That(condition.Evaluate(new FieldValues(trigger)), Is.EqualTo(expected));
    }


    [TestCase(nameof(StartedByTriggerProperties.CheckTriggerData))]
    [TestCase(nameof(StartedByTriggerProperties.Field))]
    [TestCase(nameof(StartedByTriggerProperties.Operator))]
    [TestCase(nameof(StartedByTriggerProperties.Value))]
    public void Properties_TriggerDataPropertiesHiddenForTriggersWithoutData(string propertyName)
    {
        var attribute = typeof(StartedByTriggerProperties).GetProperty(propertyName)!.GetCustomAttribute<VisibleIfTriggerHasDataAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(attribute?.PropertyName, Is.EqualTo(nameof(StartedByTriggerProperties.Trigger)));
            Assert.That(typeof(TriggerHasDataVisibilityCondition).GetCustomAttribute<VisibilityConditionAttributeAttribute>()!.AttributeType,
                Is.EqualTo(typeof(VisibleIfTriggerHasDataAttribute)));
        });
    }


    [Test]
    public void Properties_FieldDependsOnTrigger()
    {
        var field = typeof(StartedByTriggerProperties).GetProperty(nameof(StartedByTriggerProperties.Field))!;
        var configuration = field.GetCustomAttribute<FormComponentConfigurationAttribute>()!;
        var dropDown = field.GetCustomAttribute<DropDownComponentAttribute>()!;
        var trigger = typeof(StartedByTriggerProperties).GetProperty(nameof(StartedByTriggerProperties.Trigger))!.GetCustomAttribute<DropDownComponentAttribute>()!;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.ConfiguratorType, Is.EqualTo(typeof(TriggerDataFieldConfigurator)));
            Assert.That(configuration.DependencyFieldNames, Is.EqualTo(new[] { nameof(StartedByTriggerProperties.Trigger) }));
            Assert.That(dropDown.DataProviderType, Is.EqualTo(typeof(TriggerDataFieldOptionsProvider)));
            Assert.That(trigger.DataProviderType, Is.EqualTo(typeof(TriggerOptionsProvider)));
            // Dependencies only work on properties with a lower order.
            Assert.That(trigger.Order, Is.LessThan(dropDown.Order));
        });
    }


    [Test]
    public void Properties_ValueHiddenForEmptyChecks()
    {
        var conditions = typeof(StartedByTriggerProperties).GetProperty(nameof(StartedByTriggerProperties.Value))!
            .GetCustomAttributes<VisibleIfNotEqualToAttribute>()
            .Select(attribute => (attribute.PropertyName, attribute.CompareToValue))
            .ToList();

        Assert.That(conditions, Is.EquivalentTo(new[]
        {
            (nameof(StartedByTriggerProperties.Operator), nameof(TriggerDataOperator.IsEmpty)),
            (nameof(StartedByTriggerProperties.Operator), (object)nameof(TriggerDataOperator.IsNotEmpty))
        }));
    }


    private static RegisteredTrigger? Find(string identifier) => RegisteredTriggers.Find(triggers, identifier);


    private static StartedByTriggerProperties DataCheck(string trigger, string field, string comparisonOperator, string value) => new()
    {
        Trigger = trigger,
        CheckTriggerData = true,
        Field = field,
        Operator = comparisonOperator,
        Value = value
    };


    /// <summary>
    /// The public constructor resolves services from the Xperience container, so the component is created without it.
    /// </summary>
    private static DropDownComponent CreateDropDown()
    {
        var component = (DropDownComponent)RuntimeHelpers.GetUninitializedObject(typeof(DropDownComponent));
        typeof(DropDownComponent).GetProperty(nameof(DropDownComponent.Properties))!.SetValue(component, new DropDownProperties());

        return component;
    }


    private sealed class FieldValues(string? trigger) : IFormFieldValueProvider
    {
        public bool TryGet<TValue>(string fieldName, out TValue value)
        {
            if (fieldName == nameof(StartedByTriggerProperties.Trigger) && trigger is TValue typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }


    internal sealed class ResourceLocalization : ILocalizationService
    {
        private static readonly ResourceManager resources = new(typeof(AutomationExtensionsResources));


        public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true) =>
            resources.GetString(resourceKey, CultureInfo.InvariantCulture) ?? resourceKey;


        public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) => inputText;


        public string LocalizeExpression(string expression, string? culture = null, bool encode = false,
            Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) => expression;
    }
}
