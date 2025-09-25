using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExcelBotCs.Attributes;
using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExcelBotCs.Filters;

public sealed class RoleRedactionResultFilter(ICurrentMemberAccessor currentMemberAccessor)
    : IAsyncResultFilter
{
    private readonly ICurrentMemberAccessor _current = currentMemberAccessor;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Only process object results
        if (context.Result is ObjectResult { Value: not null } objectResult)
        {
            var member = await _current.GetCurrentAsync(); // null for anonymous or not found
            RedactObjectGraph(objectResult.Value, member, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }

        await next();
    }

    private static void RedactObjectGraph(object obj, Member? current, HashSet<object> visited)
    {
        if (obj is null)
            return;

        // Avoid infinite loops on cyclic graphs
        if (!ShouldRecurseInto(obj))
            return;
        if (!visited.Add(obj))
            return;

        var type = obj.GetType();

        // Collections: handle elements
        if (obj is IEnumerable enumerable && type != typeof(string))
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                    RedactObjectGraph(item, current, visited);
            }

            return;
        }

        // POCOs: inspect writable public instance properties
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;

            // Skip indexers
            if (prop.GetIndexParameters().Length > 0) continue;

            var value = prop.GetValue(obj);

            // Role checks
            if (IsProtectedByAdmin(prop) && !IsAdmin(current))
            {
                TryNullOut(obj, prop);
                continue; // Don’t descend into it
            }

            if (IsProtectedByMember(prop) && !IsMember(current))
            {
                TryNullOut(obj, prop);
                continue;
            }

            // Recurse into nested objects/collections
            if (value is not null)
                RedactObjectGraph(value, current, visited);
        }
    }

    private static bool IsProtectedByAdmin(PropertyInfo p) =>
        p.IsDefined(typeof(RequiresAdminRoleAttribute), inherit: true);

    private static bool IsProtectedByMember(PropertyInfo p) =>
        p.IsDefined(typeof(RequiresMemberRoleAttribute), inherit: true);

    private static bool IsAdmin(Member? m) => m?.IsAdmin == true;
    private static bool IsMember(Member? m) => m?.IsMember == true;

    private static void TryNullOut(object target, PropertyInfo prop)
    {
        if (!prop.CanWrite)
            return;

        var t = prop.PropertyType;
        // Only null-out if it’s a reference type or a nullable value type
        if (!t.IsValueType || Nullable.GetUnderlyingType(t) is not null)
        {
            prop.SetValue(target, null);
        }
        // If it’s a non-nullable value type, consider redesigning that property to be nullable
        // if you want it to be hidden, or adjust logic to set a default/redacted value.
    }

    private static bool ShouldRecurseInto(object obj)
    {
        var t = obj.GetType();
        if (t == typeof(string))
            return false;

        if (t == typeof(DateTime))
            return false;

        if (t.IsPrimitive)
            return false;

        if (t.IsEnum)
            return false;

        return true;
    }

    // Reference equality comparer to track visited object graphs
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}