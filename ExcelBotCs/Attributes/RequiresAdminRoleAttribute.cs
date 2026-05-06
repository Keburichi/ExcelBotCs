namespace ExcelBotCs.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RequiresAdminRoleAttribute : Attribute
{
}