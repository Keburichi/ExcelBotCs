namespace ExcelBotCs.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class OptionsSectionAttribute : Attribute
{
    public string Name { get; }
    
    public OptionsSectionAttribute(string name) => Name = name;
}