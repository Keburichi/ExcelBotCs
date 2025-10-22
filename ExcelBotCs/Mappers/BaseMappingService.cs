using System.Reflection;
using ExcelBotCs.Attributes.Mapping;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public abstract class BaseMappingService<Dto, Entity> where Dto : BaseDto where Entity : BaseEntity
{
    public Dto ToDto(Entity entity)
    {
        var dto = Activator.CreateInstance<Dto>();
        
        var entityProperties = entity.GetType().GetProperties();

        MapAttributes(entityProperties, entity, dto);

        return dto;
    }

    public Entity ToEntity(Dto dto)
    {
        var entity = Activator.CreateInstance<Entity>();
        
        var dtoProperties = entity.GetType().GetProperties();

        MapAttributes(dtoProperties, dto, entity);

        return entity;
    }

    private void MapAttributes(PropertyInfo[] properties, object source, object target)
    {
        foreach (var property in properties)
        {
            // Check if the ignore mapping attribute is set
            var attributes = property.GetCustomAttributes(true);
            
            if(attributes.Any(x => x.GetType() == typeof(IgnoreMappingAttribute)))
                continue;

            var sourceProperty = source.GetType().GetProperty(property.Name);
            if (sourceProperty == null)
                continue;
            
            var targetProperty = target.GetType().GetProperty(property.Name);
            if (targetProperty == null)
                continue;
            
            // Check that the types are identical
            if(sourceProperty.PropertyType != targetProperty.PropertyType)
                continue;
            
            targetProperty.SetValue(target, sourceProperty.GetValue(source));
        }
    }
}