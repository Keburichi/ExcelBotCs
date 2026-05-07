using ExcelBotCs.Extensions;

namespace ExcelBotCs.Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Fact]
    public void IsNullOrEmpty_Null_ReturnsTrue()
    {
        List<string> sut = null;
        sut.IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_Empty_ReturnsTrue()
    {
        var sut = new List<string>();
        sut.IsNullOrEmpty().ShouldBeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_NotEmpty_ReturnsFalse()
    {
        var sut = new List<string> { "test" };
        sut.IsNullOrEmpty().ShouldBeFalse();
    }
}
