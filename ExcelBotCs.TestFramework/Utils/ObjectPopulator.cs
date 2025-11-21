using System.Collections;
using System.Globalization;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.TestFramework.Utils;

/// <summary>
///     Extension methods for populating objects with random test data using reflection
/// </summary>
public static class ObjectPopulator
{
    private const int MinListSize = 1;
    private const int MaxListSize = 5;
    private const int MaxRecursionDepth = 5;
    private static readonly Random Random = new();

    /// <summary>
    ///     Populates all properties of an object with random test data
    /// </summary>
    /// <typeparam name="T">The type of object to populate</typeparam>
    /// <param name="obj">The object instance to populate</param>
    /// <param name="recursionDepth">Current recursion depth (used internally to prevent infinite recursion)</param>
    /// <returns>The populated object</returns>
    public static T PopulateWithRandomData<T>(this T obj, int recursionDepth = 0) where T : class
    {
        if (obj == null || recursionDepth >= MaxRecursionDepth)
            return obj;

        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetSetMethod() != null)
            .Where(p => !HasBsonIdAttribute(p)); // Skip properties with [BsonId] attribute

        foreach (var property in properties)
            try
            {
                var value = GenerateRandomValue(property, recursionDepth);
                if (value != null || IsNullableType(property.PropertyType)) property.SetValue(obj, value);
            }
            catch
            {
                // Skip properties that can't be set (e.g., computed properties)
            }

        return obj;
    }

    /// <summary>
    ///     Checks if a property has the [BsonId] attribute
    /// </summary>
    private static bool HasBsonIdAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<BsonIdAttribute>() != null;
    }

    private static object? GenerateRandomValue(PropertyInfo property, int recursionDepth)
    {
        var propertyType = property.PropertyType;

        // Handle nullable types - unwrap to get the underlying type
        var underlyingType = Nullable.GetUnderlyingType(propertyType);
        if (underlyingType != null) propertyType = underlyingType;

        // String - property name + UUID
        if (propertyType == typeof(string)) return $"{property.Name}_{Guid.NewGuid()}";

        // Boolean
        if (propertyType == typeof(bool)) return Random.Next(0, 2) == 1;

        // Integer types
        if (propertyType == typeof(int)) return Random.Next(1, 1000);

        if (propertyType == typeof(long)) return (long)Random.Next(1, 1000);

        if (propertyType == typeof(short)) return (short)Random.Next(1, 1000);

        if (propertyType == typeof(byte)) return (byte)Random.Next(0, 256);

        // Floating point types
        if (propertyType == typeof(double)) return Random.NextDouble() * 1000;

        if (propertyType == typeof(float)) return (float)(Random.NextDouble() * 1000);

        if (propertyType == typeof(decimal)) return (decimal)(Random.NextDouble() * 1000);

        // DateTime
        if (propertyType == typeof(DateTime))
        {
            var start = new DateTime(2020, 1, 1);
            var range = (DateTime.Now.AddYears(2) - start).Days;
            return start.AddDays(Random.Next(range));
        }

        // TimeSpan
        if (propertyType == typeof(TimeSpan)) return TimeSpan.FromMinutes(Random.Next(1, 1440)); // 1 minute to 24 hours

        // Guid
        if (propertyType == typeof(Guid)) return Guid.NewGuid();

        // Enums
        if (propertyType.IsEnum)
        {
            var enumValues = Enum.GetValues(propertyType);
            return enumValues.GetValue(Random.Next(enumValues.Length));
        }

        // Lists/Collections
        if (IsListType(propertyType)) return GenerateRandomList(propertyType, recursionDepth);

        // Complex objects (recursively populate)
        if (propertyType.IsClass && !propertyType.IsAbstract)
            try
            {
                var instance = Activator.CreateInstance(propertyType);
                if (instance != null) return ((dynamic)instance).PopulateWithRandomData(recursionDepth + 1);
            }
            catch
            {
                // Can't instantiate - skip
            }

        return null;
    }

    private static object? GenerateRandomList(Type listType, int recursionDepth)
    {
        // Get the element type
        Type? elementType = null;

        if (listType.IsGenericType)
        {
            var genericArgs = listType.GetGenericArguments();
            if (genericArgs.Length > 0) elementType = genericArgs[0];
        }

        if (elementType == null)
            return null;

        // Create the list
        var listTypeToCreate = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listTypeToCreate) as IList;

        if (list == null)
            return null;

        // Generate random number of elements
        var count = Random.Next(MinListSize, MaxListSize + 1);

        for (var i = 0; i < count; i++)
        {
            object? element = null;

            // Handle complex types directly rather than through FakePropertyInfo
            if (elementType.IsClass && !elementType.IsAbstract && elementType != typeof(string))
            {
                try
                {
                    element = Activator.CreateInstance(elementType);
                    if (element != null)
                    {
                        // Use reflection to call PopulateWithRandomData
                        var method = typeof(ObjectPopulator).GetMethod(nameof(PopulateWithRandomData));
                        var genericMethod = method?.MakeGenericMethod(elementType);
                        element = genericMethod?.Invoke(null, new[] { element, recursionDepth + 1 });
                    }
                }
                catch
                {
                    // Can't instantiate - skip
                }
            }
            else
            {
                // For primitive types, use the existing logic
                var elementProperty = new FakePropertyInfo(elementType);
                element = GenerateRandomValue(elementProperty, recursionDepth);
            }

            if (element != null) list.Add(element);
        }

        return list;
    }

    private static bool IsListType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericTypeDef = type.GetGenericTypeDefinition();
        return genericTypeDef == typeof(List<>) ||
               genericTypeDef == typeof(IList<>) ||
               genericTypeDef == typeof(ICollection<>) ||
               genericTypeDef == typeof(IEnumerable<>);
    }

    private static bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }

    /// <summary>
    ///     Helper class to create a fake PropertyInfo for list elements
    /// </summary>
    private class FakePropertyInfo : PropertyInfo
    {
        private readonly Type _propertyType;

        public FakePropertyInfo(Type propertyType)
        {
            _propertyType = propertyType;
        }

        public override Type PropertyType => _propertyType;
        public override string Name => "Element";
        public override bool CanWrite => true;
        public override bool CanRead => true;

        // Required overrides (not used in our implementation)
        public override PropertyAttributes Attributes => PropertyAttributes.None;
        public override Type? DeclaringType => null;
        public override Type? ReflectedType => null;

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return Array.Empty<MethodInfo>();
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return Array.Empty<ParameterInfo>();
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            return null;
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index,
            CultureInfo? culture)
        {
            return null;
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder,
            object?[]? index, CultureInfo? culture)
        {
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.Empty<object>();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return Array.Empty<object>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }
    }
}