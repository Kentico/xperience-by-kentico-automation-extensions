using Kentico.Xperience.Labs.AutomationExperiments.Triggers.ContactDateAnniversary;

namespace Kentico.Xperience.Labs.AutomationExperiments.Tests.Triggers;

public class AnniversaryCalendarTests
{
    [Test]
    public void GetMatchingMonthDays_RegularDate_ReturnsSameMonthDay()
    {
        var monthDays = AnniversaryCalendar.GetMatchingMonthDays(new DateOnly(2026, 10, 4));

        Assert.That(monthDays, Is.EqualTo(new[] { (10, 4) }));
    }


    [Test]
    public void GetMatchingMonthDays_February28InCommonYear_IncludesFebruary29()
    {
        var monthDays = AnniversaryCalendar.GetMatchingMonthDays(new DateOnly(2027, 2, 28));

        Assert.That(monthDays, Is.EqualTo(new[] { (2, 28), (2, 29) }));
    }


    [Test]
    public void GetMatchingMonthDays_February28InLeapYear_ExcludesFebruary29()
    {
        var monthDays = AnniversaryCalendar.GetMatchingMonthDays(new DateOnly(2028, 2, 28));

        Assert.That(monthDays, Is.EqualTo(new[] { (2, 28) }));
    }


    [TestCase(1990, 2026, 36)]
    [TestCase(2026, 2026, 0)]
    public void GetYears_OriginalNotAfterTarget_ReturnsYears(int originalYear, int targetYear, int expectedYears)
    {
        int? years = AnniversaryCalendar.GetYears(new DateOnly(originalYear, 5, 1), new DateOnly(targetYear, 5, 1));

        Assert.That(years, Is.EqualTo(expectedYears));
    }


    [Test]
    public void GetYears_OriginalAfterTargetYear_ReturnsNull()
    {
        int? years = AnniversaryCalendar.GetYears(new DateOnly(2027, 5, 1), new DateOnly(2026, 5, 1));

        Assert.That(years, Is.Null);
    }
}
