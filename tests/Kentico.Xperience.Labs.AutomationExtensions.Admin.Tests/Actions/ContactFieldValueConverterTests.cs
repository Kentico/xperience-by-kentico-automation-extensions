using Kentico.Xperience.Labs.AutomationExtensions.Actions.SetContactField;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.Actions;

public class ContactFieldValueConverterTests
{
    [Test]
    public void Text_KeptAsIs() => AssertConverted("AcmeLastCampaign", " Spring ", " Spring ");


    [Test]
    public void Text_LongerThanSizeIsTruncated()
    {
        var result = Convert("AcmeLastCampaign", "Spring sale 2026");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.EqualTo("Spring sal"));
            Assert.That(result.Truncated, Is.True);
        });
    }


    [Test]
    public void Text_TruncationDoesNotSplitSurrogatePair()
    {
        var result = Convert("AcmeLastCampaign", "123456789😀x");

        Assert.That(result.Value, Is.EqualTo("123456789"));
    }


    [Test]
    public void Text_ExactSizeIsNotTruncated() => Assert.That(Convert("AcmeLastCampaign", "1234567890").Truncated, Is.False);


    [Test]
    public void LongText_NotTruncated()
    {
        string text = new('x', 10000);
        var result = Convert("AcmeNotes", text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(text));
            Assert.That(result.Truncated, Is.False);
        });
    }


    [TestCase("AcmeLastCampaign")]
    [TestCase("AcmeNotes")]
    [TestCase("AcmeCounter")]
    [TestCase("AcmeRevenue")]
    [TestCase("AcmeRating")]
    [TestCase("AcmeWeight")]
    [TestCase("AcmeVip")]
    [TestCase("AcmeFirstPurchase")]
    [TestCase("AcmeLastVisit")]
    public void Empty_ClearsField(string fieldName)
    {
        var result = Convert(fieldName, fieldName is "AcmeLastCampaign" or "AcmeNotes" ? string.Empty : "  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.Null);
        });
    }


    [Test]
    public void Empty_RequiredTextFieldGetsEmptyText() => AssertConverted("AcmeRequiredCode", string.Empty, string.Empty);


    [Test]
    public void Empty_RequiredNonTextFieldFails() => AssertFailed("AcmeRequiredFlag", " ");


    [TestCase("42", 42)]
    [TestCase(" -7 ", -7)]
    [TestCase("+3", 3)]
    [TestCase("2147483647", int.MaxValue)]
    public void Integer_Valid(string text, int expected) => AssertConverted("AcmeCounter", text, expected);


    [TestCase("1.5")]
    [TestCase("1,000")]
    [TestCase("2147483648")]
    [TestCase("abc")]
    [TestCase("1e3")]
    public void Integer_Invalid(string text) => AssertFailed("AcmeCounter", text);


    [TestCase("9223372036854775807", long.MaxValue)]
    [TestCase("-5", -5L)]
    public void LongInteger_Valid(string text, long expected) => AssertConverted("AcmeRevenue", text, expected);


    [TestCase("9223372036854775808")]
    [TestCase("1.0")]
    public void LongInteger_Invalid(string text) => AssertFailed("AcmeRevenue", text);


    [TestCase("12.75", "12.75")]
    [TestCase("-0.5", "-0.5")]
    [TestCase("1e2", "100")]
    public void Decimal_Valid(string text, string expected) => AssertConverted("AcmeRating", text, decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));


    [TestCase("12,75")]
    [TestCase("1,000.5")]
    [TestCase("abc")]
    public void Decimal_Invalid(string text) => AssertFailed("AcmeRating", text);


    [TestCase("2.5", 2.5)]
    [TestCase("-1E3", -1000d)]
    public void Double_Valid(string text, double expected) => AssertConverted("AcmeWeight", text, expected);


    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("1e400")]
    [TestCase("2,5")]
    public void Double_Invalid(string text) => AssertFailed("AcmeWeight", text);


    [TestCase("true", true)]
    [TestCase("TRUE", true)]
    [TestCase("1", true)]
    [TestCase("false", false)]
    [TestCase("False", false)]
    [TestCase(" 0 ", false)]
    public void Boolean_Valid(string text, bool expected) => AssertConverted("AcmeVip", text, expected);


    [TestCase("yes")]
    [TestCase("2")]
    [TestCase("on")]
    public void Boolean_Invalid(string text) => AssertFailed("AcmeVip", text);


    [TestCase("2026-10-11")]
    [TestCase("2026-10-11T14:30:00")]
    [TestCase("2026-10-11 14:30")]
    public void Date_ValidDropsTime(string text) => AssertConverted("AcmeFirstPurchase", text, new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Unspecified));


    [TestCase("2026-10-11T14:30:15", 14, 30, 15)]
    [TestCase("2026-10-11T14:30", 14, 30, 0)]
    [TestCase("2026-10-11", 0, 0, 0)]
    [TestCase("2026-10-11T14:30:15.5", 14, 30, 15)]
    public void DateTime_Valid(string text, int hour, int minute, int second)
    {
        var result = Convert("AcmeLastVisit", text);

        Assert.That(result.Value is DateTime value && value.Date == new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Unspecified) && value.Hour == hour && value.Minute == minute && value.Second == second, Is.True);
    }


    [TestCase("11/10/2026")]
    [TestCase("10/11/2026")]
    [TestCase("2026-13-01")]
    [TestCase("2026-10-11T14:30:00Z")]
    [TestCase("2026-10-11T14:30:00+02:00")]
    [TestCase("tomorrow")]
    public void DateTime_Invalid(string text) => Assert.Multiple(() =>
    {
        AssertFailed("AcmeLastVisit", text);
        AssertFailed("AcmeFirstPurchase", text);
    });


    [Test]
    public void Failure_DoesNotContainValue()
    {
        var result = Convert("AcmeCounter", "john@example.com");

        Assert.That(result.Error, Does.Not.Contain("john"));
    }


    [Test]
    public void Format_UsesInvariantIsoValues() => Assert.Multiple(() =>
    {
        Assert.That(ContactFieldValueConverter.Format(null), Is.Empty);
        Assert.That(ContactFieldValueConverter.Format(DBNull.Value), Is.Empty);
        Assert.That(ContactFieldValueConverter.Format(12.5m), Is.EqualTo("12.5"));
        Assert.That(ContactFieldValueConverter.Format(0.1d), Is.EqualTo("0.1"));
        Assert.That(ContactFieldValueConverter.Format(-3), Is.EqualTo("-3"));
        Assert.That(ContactFieldValueConverter.Format(true), Is.EqualTo("true"));
        Assert.That(ContactFieldValueConverter.Format(new DateTime(2026, 10, 11, 8, 5, 3, DateTimeKind.Unspecified)), Is.EqualTo("2026-10-11T08:05:03"));
        Assert.That(ContactFieldValueConverter.Format(new DateTime(2026, 10, 11, 8, 5, 3, DateTimeKind.Unspecified), dateOnly: true), Is.EqualTo("2026-10-11"));
        Assert.That(ContactFieldValueConverter.Format(Guid.Empty), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
    });


    [Test]
    public void Format_IndependentOfCurrentCulture()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            Assert.Multiple(() =>
            {
                Assert.That(ContactFieldValueConverter.Format(1.5m), Is.EqualTo("1.5"));
                Assert.That(Convert("AcmeRating", "1.5").Value, Is.EqualTo(1.5m));
            });
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }


    [Test]
    public void Format_RoundTripsThroughConvert() => Assert.Multiple(() =>
    {
        AssertConverted("AcmeWeight", ContactFieldValueConverter.Format(0.1d + 0.2d), 0.1d + 0.2d);
        AssertConverted("AcmeLastVisit", ContactFieldValueConverter.Format(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)), new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified));
        AssertConverted("AcmeVip", ContactFieldValueConverter.Format(false), false);
    });


    [TestCase(null, ExpectedResult = true)]
    [TestCase("", ExpectedResult = true)]
    [TestCase(" ", ExpectedResult = false)]
    [TestCase(0, ExpectedResult = false)]
    [TestCase(false, ExpectedResult = false)]
    public bool IsEmpty(object? value) => ContactFieldValueConverter.IsEmpty(value);


    [Test]
    public void IsEmpty_DbNull() => Assert.That(ContactFieldValueConverter.IsEmpty(DBNull.Value), Is.True);


    private static ConversionResult Convert(string fieldName, string text) =>
        ContactFieldValueConverter.Convert(text, ContactFieldResolverTests.GetField(fieldName));


    private static void AssertConverted(string fieldName, string text, object expected)
    {
        var result = Convert(fieldName, text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.Value?.GetType(), Is.EqualTo(expected.GetType()));
        });
    }


    private static void AssertFailed(string fieldName, string text)
    {
        var result = Convert(fieldName, text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False, text);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Error, Is.Not.Empty);
        });
    }
}
