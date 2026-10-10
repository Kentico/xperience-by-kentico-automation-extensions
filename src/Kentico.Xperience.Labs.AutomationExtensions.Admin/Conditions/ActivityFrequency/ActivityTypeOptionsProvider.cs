using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.ActivityFrequency;

/// <summary>
/// Supplies the enabled activity types to the activity type drop-down of <see cref="ActivityFrequencyProperties"/>.
/// </summary>
internal sealed class ActivityTypeOptionsProvider(ActivityTypeResolver activityTypeResolver) : IDropDownOptionsProvider
{
    private readonly ActivityTypeResolver activityTypeResolver = activityTypeResolver;


    /// <inheritdoc />
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        (await activityTypeResolver.GetEnabledTypes(CancellationToken.None))
            .Select(type => new DropDownOptionItem
            {
                Value = type.Name,
                Text = type.DisplayName
            })
            .ToList();
}
