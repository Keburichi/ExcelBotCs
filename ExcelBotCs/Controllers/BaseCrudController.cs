using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

public abstract class BaseCrudController<Dto, Entity> : AuthorizedController where Dto : BaseDto where Entity : BaseEntity
{
    protected readonly ILogger _logger;
    private readonly BaseDatabaseService<Entity> _databaseService;

    protected BaseCrudController(ILogger logger, BaseDatabaseService<Entity> databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    protected virtual Task<ActionResult<List<Dto>>> OnBeforeGetAllAsync() => Task.FromResult<ActionResult<List<Dto>>>(null);
    protected virtual Task<ActionResult<Dto>> OnBeforeGetAsync(Dto entity) => Task.FromResult<ActionResult<Dto>>(null);
    protected virtual Task<ActionResult<Dto>> OnBeforePutAsync(Dto entity) => Task.FromResult<ActionResult<Dto>>(null);
    protected virtual Task<ActionResult<Dto>> OnBeforePostAsync(Dto entity) => Task.FromResult<ActionResult<Dto>>(null);
    protected virtual Task<ActionResult<Dto>> OnAfterPostAsync(Dto entity) => Task.FromResult<ActionResult<Dto>>(null);
    protected virtual Task<ActionResult<Dto>> OnBeforeDeleteAsync(Dto entity) => Task.FromResult<ActionResult<Dto>>(null);

    [HttpGet]
    public async Task<ActionResult<List<Dto>>> GetEntities()
    {
        var pre = await OnBeforeGetAllAsync();
        if (pre is not null)
            return pre;

        return await _databaseService.GetAsync();
    }


    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Dto>> GetEntity(string id)
    {
        var entity = await _databaseService.GetAsync(id);

        if (entity is null)
            return NotFound();

        var pre = await OnBeforeGetAsync(entity);
        if (pre is not null)
            return pre;

        return entity;
    }

    [HttpPost]
    public async Task<ActionResult<Dto>> PostEntity(Dto entity)
    {
        var pre = await OnBeforePostAsync(entity);
        if (pre is not null)
            return pre;

        await _databaseService.CreateAsync(entity);
        return CreatedAtAction(nameof(GetEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<Dto>> UpdateEntity(string id, Dto updatedEntity)
    {
        _logger.LogInformation("Updating entity with id: {id}", id);

        var dbEntity = await _databaseService.GetAsync(id);

        if (dbEntity is null)
            return NotFound();

        updatedEntity.Id = dbEntity.Id;

        var pre = await OnBeforePutAsync(updatedEntity);
        if (pre is not null)
            return pre;

        // Update only properties that were passed in
        var properties = typeof(Dto).GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(updatedEntity);
            if (value != null && !value.Equals(property.GetValue(dbEntity)))
            {
                property.SetValue(dbEntity, value);
            }
        }

        dbEntity.EditDate = DateTime.UtcNow;

        await _databaseService.UpdateAsync(id, dbEntity);
        
        var after = await OnAfterPostAsync(dbEntity);
        if (after is not null)
            return after;
        
        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult<Dto>> DeleteEntity(string id)
    {
        var entity = await _databaseService.GetAsync(id);

        if (entity is null)
            return NotFound();

        var pre = await OnBeforeDeleteAsync(entity);
        if (pre is not null)
            return pre;

        await _databaseService.DeleteAsync(id);
        return NoContent();
    }
}