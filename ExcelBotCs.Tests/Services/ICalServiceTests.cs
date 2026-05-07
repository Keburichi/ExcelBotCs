using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.TestFramework.TestData;
using Ical.Net;
using Ical.Net.Serialization;

namespace ExcelBotCs.Tests.Services;

public class ICalServiceTests
{
    private readonly IICalService _iCalService;

    public ICalServiceTests()
    {
        _iCalService = new ICalService();
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void IsRecurringEvent_ICalIsEmpty_ReturnsFalse(string? ical)
    {
        _iCalService.IsRecurringEvent(ical).ShouldBeFalse();
    }

    // [Fact]
    public void IsRecurringEvent_InvalidIcal_ReturnsFalse()
    {
        var ical = "I'm invalid";

        _iCalService.IsRecurringEvent(ical).ShouldBeFalse();
    }

    [Fact]
    public void IsRecurringEvent_ICalContainsNoEvent_ReturnsFalse()
    {
        var calendar = new Calendar();
        calendar.Name = "Test";
        var serializer = new CalendarSerializer();
        var ical = serializer.SerializeToString(calendar);

        _iCalService.IsRecurringEvent(ical).ShouldBeFalse();
    }

    [Fact]
    public void IsRecurringEvent_EventIsNotRecurring_ReturnsFalse()
    {
        var sut = new Event
        {
            Name = "Test",
            StartDate = DateTime.UtcNow,
            SignupType = SignupType.SingleEvent
        };

        var ical = _iCalService.GenerateICalString(sut);

        _iCalService.IsRecurringEvent(ical).ShouldBeFalse();
    }


    [Theory]
    [MemberData(nameof(NullOrEmptyStringData.Values), MemberType = typeof(NullOrEmptyStringData))]
    public void IsRecurringEnding_ICalIsEmpty_ReturnsFalse(string? ical)
    {
        _iCalService.IsRecurrenceEnding(ical).ShouldBeFalse();
    }
}
