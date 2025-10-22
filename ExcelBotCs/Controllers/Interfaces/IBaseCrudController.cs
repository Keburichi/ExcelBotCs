using ExcelBotCs.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IBaseCrudController<TDto> where TDto : BaseDto
{
    public Task<ActionResult<List<TDto>>> GetEntities();
    public Task<ActionResult<TDto>> GetEntity(string id);
    public Task<ActionResult<TDto>> CreateEntity(TDto entity);
    public Task<ActionResult<TDto>> UpdateEntity(string id, TDto updatedEntity);
    public Task<ActionResult<TDto>> DeleteEntity(string id);
}