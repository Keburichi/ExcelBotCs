using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Extensions;

public static class DtoExtensions
{
    public static void UpdateUpdatedAttributes(this BaseDto updatedDto, BaseDto target)
    {
        var properties = updatedDto.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(updatedDto);
            if (value != null && !value.Equals(property.GetValue(target)))
            {
                property.SetValue(target, value);
            }
        }
    }
}