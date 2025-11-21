using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.TestFramework.Attributes;
using Ical.Net;
using Ical.Net.Serialization;

namespace ExcelBotCs.Tests.Services;

[TestFixture]
public class ICalServiceTests
{
    private readonly IICalService _iCalService;

    public ICalServiceTests()
    {
        _iCalService = new ICalService();
    }

    [TestIsNullOrEmptyString]
    public void IsRecurringEvent_ICalIsEmpty_ReturnsFalse(string ical)
    {
        Assert.That(() => _iCalService.IsRecurringEvent(ical), Is.False);
    }

    // [Test]
    public void IsRecurringEvent_InvalidIcal_ReturnsFalse()
    {
        var ical = "I'm invalid";

        Assert.That(() => _iCalService.IsRecurringEvent(ical), Is.False);
    }

    [Test]
    public void IsRecurringEvent_ICalContainsNoEvent_ReturnsFalse()
    {
        var calendar = new Calendar();
        calendar.Name = "Test";
        var serializer = new CalendarSerializer();
        var ical = serializer.SerializeToString(calendar);

        Assert.That(() => _iCalService.IsRecurringEvent(ical), Is.False);
    }

    [Test]
    public void IsRecurringEvent_EventIsNotRecurring_ReturnsFalse()
    {
        var sut = new Event
        {
            Name = "Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            SignupType = SignupType.SingleEvent
        };

        var ical = _iCalService.GenerateICalString(sut);

        Assert.That(() => _iCalService.IsRecurringEvent(ical), Is.False);
    }


    [TestIsNullOrEmptyString]
    public void IsRecurringEnding_ICalIsEmpty_ReturnsFalse(string ical)
    {
        Assert.That(() => _iCalService.IsRecurrenceEnding(ical), Is.False);
    }
}