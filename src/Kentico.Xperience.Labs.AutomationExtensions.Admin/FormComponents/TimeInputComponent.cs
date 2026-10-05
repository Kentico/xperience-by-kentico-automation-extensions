using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.FormComponents;

[assembly: RegisterFormComponent(
    TimeInputComponent.IDENTIFIER,
    typeof(TimeInputComponent),
    "Time input")]

namespace Kentico.Xperience.Labs.AutomationExtensions.FormComponents;

/// <summary>
/// Assigns <see cref="TimeInputComponent"/> to a string property holding a time in HH:mm format.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TimeInputComponentAttribute : FormComponentAttribute
{
}


/// <summary>
/// HTML5 time input. Stores the value as "HH:mm" (24-hour).
/// </summary>
[ComponentAttribute(typeof(TimeInputComponentAttribute))]
public sealed class TimeInputComponent : FormComponent<FormComponentProperties, TimeInputClientProperties, string>
{
    public const string IDENTIFIER = "Kentico.Xperience.Labs.AutomationExtensions.TimeInput";

    // Client resolves "TimeInput" + "FormComponent" suffix => TimeInputFormComponent export
    public override string ClientComponentName => "@kentico/xperience-admin-labs-automation-extensions/TimeInput";
}


public sealed class TimeInputClientProperties : FormComponentClientProperties<string>
{
}
