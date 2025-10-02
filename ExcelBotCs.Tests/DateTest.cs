namespace ExcelBotCs.Tests;

[TestFixture]
public class DateTest
{
    [Test]
    public void Test()
    {
        var date = DateTime.Now;
        Console.WriteLine(((DateTimeOffset)date).ToUnixTimeSeconds());
    }
}