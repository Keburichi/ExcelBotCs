using System.Collections;

namespace ExcelBotCs.Extensions;

public static class EnumerableExtensions
{
    public static bool IsNullOrEmpty(this IEnumerable enumerable)
    {
        return enumerable == null || !enumerable.GetEnumerator().MoveNext();
    }
}