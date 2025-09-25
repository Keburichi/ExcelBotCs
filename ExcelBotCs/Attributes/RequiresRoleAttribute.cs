namespace ExcelBotCs.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class RequiresRoleAttribute(params string[] roles) : Attribute
{
    public string[] Roles { get; } = roles;
}