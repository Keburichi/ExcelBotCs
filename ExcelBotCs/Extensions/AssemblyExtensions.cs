using System.Reflection;
using ExcelBotCs.Attributes;

namespace ExcelBotCs.Extensions;

public static class AssemblyExtensions
{
    public static IEnumerable<Type> GetTypesFromInterface(this Assembly assembly, Type interfaceType)
    {
        return assembly.GetTypes().Where(type =>
            !type.IsAbstract && !type.IsInterface && type.GetInterfaces().Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == interfaceType));
    }

    public static List<OptionsAttribute> GetOptionTypes(this Assembly assembly)
    {
        var options = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t => new OptionsAttribute(){ Type = t, Attribute = t.GetCustomAttribute<OptionsSectionAttribute>() })
            .Where(x => x.Attribute is not null)
            .ToList();

        return options;
    }
}

public class OptionsAttribute
{
    public Type Type { get; set; }
    public OptionsSectionAttribute? Attribute { get; set; }
}