namespace ExcelBotCs.Attributes.Mapping;

[AttributeUsage(AttributeTargets.Property,  AllowMultiple = false, Inherited = true)]
public class MappingNameAttribute : Attribute
{
    public string MappingName { get; set; }

    public MappingNameAttribute(string mappingName)
    {
        MappingName = mappingName;
    }
}