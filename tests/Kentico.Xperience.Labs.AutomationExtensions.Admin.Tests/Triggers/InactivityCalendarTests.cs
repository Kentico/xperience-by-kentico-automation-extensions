using System.Globalization;

using Kentico.Xperience.Labs.AutomationExtensions.Triggers.ContactInactive;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Triggers;

public class InactivityCalendarTests
{
    [Test]
    public void GetRange_ReturnsWholeLocalDayEndingAtNextMidnight()
    {
        var range = InactivityCalendar.GetRange(new DateOnly(2026, 10, 11), 7);

        Assert.Multiple(() =>
        {
            Assert.That(range.LastActivityDate, Is.EqualTo(new DateOnly(2026, 10, 4)));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.That(range.Start.Kind, Is.EqualTo(DateTimeKind.Unspecified), "ActivityCreated is server local time.");
            Assert.That(range.End.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        });
    }


    [TestCase("2026-10-11", 1, "2026-10-10")]
    [TestCase("2026-03-01", 1, "2026-02-28")]
    [TestCase("2028-03-01", 1, "2028-02-29")]
    [TestCase("2027-01-05", 7, "2026-12-29")]
    [TestCase("2026-10-11", 90, "2026-07-13")]
    [TestCase("2029-03-01", 365, "2028-03-01")]
    [TestCase("2026-10-11", 3650, "2016-10-13")]
    public void GetRange_CrossesMonthYearAndLeapDays(string today, int days, string expectedDay)
    {
        var range = InactivityCalendar.GetRange(DateOnly.Parse(today, CultureInfo.InvariantCulture), days);
        var expected = DateOnly.Parse(expectedDay, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(range.LastActivityDate, Is.EqualTo(expected));
            Assert.That(range.Start, Is.EqualTo(expected.ToDateTime(TimeOnly.MinValue)));
            Assert.That(range.End - range.Start, Is.EqualTo(TimeSpan.FromDays(1)));
        });
    }


    [Test]
    public void GetRange_LeapDayEndsAtMarchFirst()
    {
        var range = InactivityCalendar.GetRange(new DateOnly(2028, 3, 7), 7);

        Assert.That(range.End, Is.EqualTo(new DateTime(2028, 3, 1, 0, 0, 0, DateTimeKind.Unspecified)));
    }
}
