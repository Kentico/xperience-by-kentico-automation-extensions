using System.Collections.Generic;
using System.Threading.Tasks;

using CMS.Core;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Samples.DancingGoat;

/// <summary>
/// Supplies the localized demo outcomes of <see cref="CoffeeClubEnrollmentAutomationAction"/>.
/// </summary>
internal sealed class CoffeeClubEnrollmentOutcomeOptionsProvider : IDropDownOptionsProvider
{
    private readonly ILocalizationService localizationService;


    /// <summary>
    /// Initializes a new instance of the <see cref="CoffeeClubEnrollmentOutcomeOptionsProvider"/> class.
    /// </summary>
    public CoffeeClubEnrollmentOutcomeOptionsProvider(ILocalizationService localizationService)
    {
        this.localizationService = localizationService;
    }


    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        IEnumerable<DropDownOptionItem> items =
        [
            CreateItem(CoffeeClubEnrollmentOutcomes.AUTOMATIC, "dancinggoat.automation.coffeeclubenrollment.outcome.automatic"),
            CreateItem(CoffeeClubEnrollmentOutcomes.ALWAYS_SUCCEED, "dancinggoat.automation.coffeeclubenrollment.outcome.succeed"),
            CreateItem(CoffeeClubEnrollmentOutcomes.ALWAYS_FAIL, "dancinggoat.automation.coffeeclubenrollment.outcome.fail")
        ];

        return Task.FromResult(items);
    }


    private DropDownOptionItem CreateItem(string value, string resourceKey) => new()
    {
        Value = value,
        Text = localizationService.GetString(resourceKey)
    };
}
