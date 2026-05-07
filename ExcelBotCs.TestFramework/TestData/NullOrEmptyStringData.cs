namespace ExcelBotCs.TestFramework.TestData;

public static class NullOrEmptyStringData
{
    public static IEnumerable<object?[]> Values
    {
        get
        {
            yield return [null];
            yield return [string.Empty];
            yield return [" "];
            yield return ["  "];
            yield return ["   "];
            yield return ["\t"];
            yield return ["\t\t"];
            yield return ["\n"];
            yield return ["\r"];
            yield return ["\r\n"];
            yield return [" \t"];
            yield return ["\t "];
            yield return [" \t "];
            yield return [" \n"];
            yield return ["\n "];
            yield return ["\t\n"];
            yield return ["\n\t"];
            yield return [" \t\n"];
            yield return ["\t \n"];
            yield return [" \n\t"];
            yield return [" \t\n\r"];
            yield return [" \r\n\t "];
        }
    }
}
