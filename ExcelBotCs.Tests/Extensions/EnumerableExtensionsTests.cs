using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

[TestFixture]
public class EnumerableExtensionsTests
{
    [Test]
    public void IsNullOrEmpty_Null_ReturnsTrue()
    {
        List<string> sut = null;
        Assert.That(() => sut.IsNullOrEmpty(), Is.True);
    }

    [Test]
    public void IsNullOrEmpty_Empty_ReturnsTrue()
    {
        var sut = new List<string>();
        Assert.That(() => sut.IsNullOrEmpty(), Is.True);
    }

    [Test]
    public void IsNullOrEmpty_NotEmpty_ReturnsFalse()
    {
        var sut = new List<string> { "test" };
        Assert.That(() => sut.IsNullOrEmpty(), Is.False);
    }
}