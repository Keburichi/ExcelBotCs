using ExcelBotCs.Attributes.API;
using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Extensions;

public static class EntityExtensions
{
    public static void UpdateUpdatedAttributes(this BaseEntity updatedEntity, BaseEntity target)
    {
        var properties = updatedEntity.GetType().GetProperties();
        foreach (var property in properties)
        {
            // Check if the property is annotated with the IgnoreUpdate attribute
            var attributes = property.GetCustomAttributes(true);
            if(attributes.Any(x => x.GetType() == typeof(IgnoreUpdateAttribute)))
                continue;
            
            var value = property.GetValue(updatedEntity);
            if (value != null && !value.Equals(property.GetValue(target)))
            {
                property.SetValue(target, value);
            }
        }
    }
}