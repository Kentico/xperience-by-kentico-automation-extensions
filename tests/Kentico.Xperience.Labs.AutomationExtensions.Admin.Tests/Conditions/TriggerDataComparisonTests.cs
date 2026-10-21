using Kentico.Xperience.Labs.AutomationExtensions.Conditions.StartedByTrigger;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Conditions;

public class TriggerDataComparisonTests
{
    private static readonly Guid guid = new("0b0f2c4e-3a51-4f7c-9d1e-6a2b8c9d0e1f");
    private static readonly DateTime date = new(2026, 10, 11, 14, 30, 0, DateTimeKind.Utc);


    [TestCase(typeof(string), "Text")]
    [TestCase(typeof(Guid), "Text")]
    [TestCase(typeof(bool), "Text")]
    [TestCase(typeof(int), "Number")]
    [TestCase(typeof(int?), "Number")]
    [TestCase(typeof(long), "Number")]
    [TestCase(typeof(decimal?), "Number")]
    [TestCase(typeof(double), "Number")]
    [TestCase(typeof(DateTime), "Date")]
    [TestCase(typeof(DateTime?), "Date")]
    public void GetKind(Type type, string expected) => Assert.That(TriggerDataComparison.GetKind(type).ToString(), Is.EqualTo(expected));


    // Text
    [TestCase("Acme_Webinar", TriggerDataOperator.Equal, "acme_webinar", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.Equal, " acme_webinar ", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.Equal, "acme", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.NotEqual, "acme_webinar", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.NotEqual, "acme", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.Contains, "WEB", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.Contains, "shop", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.DoesNotContain, "shop", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.DoesNotContain, "web", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.StartsWith, "acme_", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.StartsWith, "webinar", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.EndsWith, "WEBINAR", true)]
    [TestCase("Acme_Webinar", TriggerDataOperator.EndsWith, "acme", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.IsEmpty, "", false)]
    [TestCase("Acme_Webinar", TriggerDataOperator.IsNotEmpty, "", true)]
    [TestCase(" ", TriggerDataOperator.IsEmpty, "", true)]
    [TestCase("", TriggerDataOperator.IsNotEmpty, "", false)]
    public void Compare_Text(string actual, TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(actual, TriggerDataValueKind.Text, comparisonOperator, value, expected);


    // Numbers
    [TestCase(5, TriggerDataOperator.Equal, "5", true)]
    [TestCase(5, TriggerDataOperator.Equal, "5.0", true)]
    [TestCase(5, TriggerDataOperator.Equal, "6", false)]
    [TestCase(5, TriggerDataOperator.NotEqual, "6", true)]
    [TestCase(5, TriggerDataOperator.NotEqual, "5", false)]
    [TestCase(15, TriggerDataOperator.Contains, "5", true)]
    [TestCase(15, TriggerDataOperator.DoesNotContain, "5", false)]
    [TestCase(15, TriggerDataOperator.StartsWith, "1", true)]
    [TestCase(15, TriggerDataOperator.EndsWith, "1", false)]
    [TestCase(5, TriggerDataOperator.GreaterThan, "4.5", true)]
    [TestCase(5, TriggerDataOperator.GreaterThan, "5", false)]
    [TestCase(5, TriggerDataOperator.GreaterThanOrEqual, "5", true)]
    [TestCase(5, TriggerDataOperator.GreaterThanOrEqual, "6", false)]
    [TestCase(-5, TriggerDataOperator.LessThan, "-4", true)]
    [TestCase(5, TriggerDataOperator.LessThan, "5", false)]
    [TestCase(5, TriggerDataOperator.LessThanOrEqual, "5", true)]
    [TestCase(5, TriggerDataOperator.LessThanOrEqual, "4", false)]
    [TestCase(0, TriggerDataOperator.IsEmpty, "", false)]
    [TestCase(0, TriggerDataOperator.IsNotEmpty, "", true)]
    public void Compare_Number(int actual, TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(actual, TriggerDataValueKind.Number, comparisonOperator, value, expected);


    [Test]
    public void Compare_OtherNumberTypes() => Assert.Multiple(() =>
    {
        Assert.That(TriggerDataComparison.Compare(12.5m, TriggerDataValueKind.Number, TriggerDataOperator.Equal, "12.50"), Is.True);
        Assert.That(TriggerDataComparison.Compare(0.1m, TriggerDataValueKind.Number, TriggerDataOperator.GreaterThan, "0.09999999999999999999"), Is.True);
        Assert.That(TriggerDataComparison.Compare(long.MaxValue, TriggerDataValueKind.Number, TriggerDataOperator.GreaterThan, "9223372036854775806"), Is.True);
        Assert.That(TriggerDataComparison.Compare(1.5d, TriggerDataValueKind.Number, TriggerDataOperator.LessThan, "1e1"), Is.True);
        Assert.That(TriggerDataComparison.Compare((int?)7, TriggerDataValueKind.Number, TriggerDataOperator.Equal, "7"), Is.True);
    });


    // Dates (time ignored)
    [TestCase(TriggerDataOperator.Equal, "2026-10-11", true)]
    [TestCase(TriggerDataOperator.Equal, "2026-10-12", false)]
    [TestCase(TriggerDataOperator.NotEqual, "2026-10-12", true)]
    [TestCase(TriggerDataOperator.NotEqual, "2026-10-11", false)]
    [TestCase(TriggerDataOperator.Contains, "T14:30", true)]
    [TestCase(TriggerDataOperator.DoesNotContain, "2025", true)]
    [TestCase(TriggerDataOperator.StartsWith, "2026-10", true)]
    [TestCase(TriggerDataOperator.EndsWith, "14:30:00", true)]
    [TestCase(TriggerDataOperator.GreaterThan, "2026-10-10", true)]
    [TestCase(TriggerDataOperator.GreaterThan, "2026-10-11", false)]
    [TestCase(TriggerDataOperator.GreaterThanOrEqual, "2026-10-11", true)]
    [TestCase(TriggerDataOperator.LessThan, "2026-10-12", true)]
    [TestCase(TriggerDataOperator.LessThan, "2026-10-11", false)]
    [TestCase(TriggerDataOperator.LessThanOrEqual, "2026-10-11", true)]
    [TestCase(TriggerDataOperator.LessThanOrEqual, "2026-10-10", false)]
    [TestCase(TriggerDataOperator.IsEmpty, "", false)]
    [TestCase(TriggerDataOperator.IsNotEmpty, "", true)]
    public void Compare_Date(TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(date, TriggerDataValueKind.Date, comparisonOperator, value, expected);


    // Missing values are never equal, greater, or less.
    [TestCase("Text", TriggerDataOperator.Equal, "x", false)]
    [TestCase("Text", TriggerDataOperator.NotEqual, "x", true)]
    [TestCase("Text", TriggerDataOperator.Contains, "x", false)]
    [TestCase("Text", TriggerDataOperator.DoesNotContain, "x", true)]
    [TestCase("Text", TriggerDataOperator.StartsWith, "x", false)]
    [TestCase("Text", TriggerDataOperator.EndsWith, "x", false)]
    [TestCase("Text", TriggerDataOperator.IsEmpty, "", true)]
    [TestCase("Text", TriggerDataOperator.IsNotEmpty, "", false)]
    [TestCase("Number", TriggerDataOperator.Equal, "0", false)]
    [TestCase("Number", TriggerDataOperator.NotEqual, "0", true)]
    [TestCase("Number", TriggerDataOperator.GreaterThan, "-1", false)]
    [TestCase("Number", TriggerDataOperator.GreaterThanOrEqual, "0", false)]
    [TestCase("Number", TriggerDataOperator.LessThan, "1", false)]
    [TestCase("Number", TriggerDataOperator.LessThanOrEqual, "0", false)]
    [TestCase("Date", TriggerDataOperator.Equal, "2026-10-11", false)]
    [TestCase("Date", TriggerDataOperator.NotEqual, "2026-10-11", true)]
    [TestCase("Date", TriggerDataOperator.GreaterThan, "2000-01-01", false)]
    [TestCase("Date", TriggerDataOperator.LessThanOrEqual, "2100-01-01", false)]
    public void Compare_Null(string kind, TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(null, Enum.Parse<TriggerDataValueKind>(kind), comparisonOperator, value, expected);


    [TestCase(true, TriggerDataOperator.Equal, "TRUE", true)]
    [TestCase(true, TriggerDataOperator.Equal, "false", false)]
    [TestCase(false, TriggerDataOperator.NotEqual, "true", true)]
    [TestCase(false, TriggerDataOperator.Contains, "als", true)]
    [TestCase(false, TriggerDataOperator.DoesNotContain, "true", true)]
    [TestCase(true, TriggerDataOperator.StartsWith, "t", true)]
    [TestCase(true, TriggerDataOperator.EndsWith, "e", true)]
    [TestCase(false, TriggerDataOperator.IsEmpty, "", false)]
    [TestCase(false, TriggerDataOperator.IsNotEmpty, "", true)]
    public void Compare_Bool(bool actual, TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(actual, TriggerDataValueKind.Text, comparisonOperator, value, expected);


    [TestCase(TriggerDataOperator.Equal, "0B0F2C4E-3A51-4F7C-9D1E-6A2B8C9D0E1F", true)]
    [TestCase(TriggerDataOperator.NotEqual, "0b0f2c4e-3a51-4f7c-9d1e-6a2b8c9d0e1f", false)]
    [TestCase(TriggerDataOperator.Contains, "3a51", true)]
    [TestCase(TriggerDataOperator.DoesNotContain, "ffff", true)]
    [TestCase(TriggerDataOperator.StartsWith, "0b0f", true)]
    [TestCase(TriggerDataOperator.EndsWith, "0e1f", true)]
    [TestCase(TriggerDataOperator.IsEmpty, "", false)]
    [TestCase(TriggerDataOperator.IsNotEmpty, "", true)]
    public void Compare_Guid(TriggerDataOperator comparisonOperator, string value, bool expected) =>
        AssertCompare(guid, TriggerDataValueKind.Text, comparisonOperator, value, expected);


    [TestCase("Number", TriggerDataOperator.Equal, "5", null)]
    [TestCase("Number", TriggerDataOperator.Equal, "1,5", "isn't a number")]
    [TestCase("Number", TriggerDataOperator.GreaterThan, "abc", "isn't a number")]
    [TestCase("Number", TriggerDataOperator.LessThan, "NaN", "isn't a number")]
    [TestCase("Number", TriggerDataOperator.Contains, "abc", null)]
    [TestCase("Date", TriggerDataOperator.Equal, "2026-10-11", null)]
    [TestCase("Date", TriggerDataOperator.Equal, "11/10/2026", "isn't a date")]
    [TestCase("Date", TriggerDataOperator.LessThanOrEqual, "2026-10-11T10:00", "isn't a date")]
    [TestCase("Date", TriggerDataOperator.StartsWith, "2026", null)]
    [TestCase("Text", TriggerDataOperator.GreaterThan, "a", "can only compare numbers and dates")]
    [TestCase("Text", TriggerDataOperator.LessThanOrEqual, "5", "can only compare numbers and dates")]
    [TestCase("Text", TriggerDataOperator.Equal, "", "no value is entered")]
    [TestCase("Text", TriggerDataOperator.Contains, "  ", "no value is entered")]
    [TestCase("Number", TriggerDataOperator.IsEmpty, "", null)]
    [TestCase("Text", TriggerDataOperator.IsNotEmpty, "ignored", null)]
    public void GetValueError(string kind, TriggerDataOperator comparisonOperator, string value, string? expectedError)
    {
        string? error = TriggerDataComparison.GetValueError(Enum.Parse<TriggerDataValueKind>(kind), comparisonOperator, value);

        if (expectedError is null)
        {
            Assert.That(error, Is.Null);
        }
        else
        {
            Assert.That(error, Does.Contain(expectedError));
        }
    }


    [Test]
    public void GetValueError_DoesNotContainValue() =>
        Assert.That(TriggerDataComparison.GetValueError(TriggerDataValueKind.Number, TriggerDataOperator.Equal, "jane@example.com"), Does.Not.Contain("jane"));


    [TestCase("Equal", true)]
    [TestCase("IsNotEmpty", true)]
    [TestCase("equal", false)]
    [TestCase("0", false)]
    [TestCase("Between", false)]
    [TestCase(null, false)]
    public void TryParseOperator(string? value, bool expected) => Assert.That(TriggerDataComparison.TryParseOperator(value, out _), Is.EqualTo(expected));


    private static void AssertCompare(object? actual, TriggerDataValueKind kind, TriggerDataOperator comparisonOperator, string value, bool expected) => Assert.Multiple(() =>
    {
        Assert.That(TriggerDataComparison.GetValueError(kind, comparisonOperator, value), Is.Null, "Test value must be valid.");
        Assert.That(TriggerDataComparison.Compare(actual, kind, comparisonOperator, value), Is.EqualTo(expected));
    });
}
