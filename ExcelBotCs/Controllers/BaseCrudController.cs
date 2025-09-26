using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

public abstract class BaseCrudController<T> : ControllerBase where T : BaseEntity
{
    protected readonly ILogger _logger;
    private readonly BaseDatabaseService<T> _databaseService;

    protected BaseCrudController(ILogger logger, BaseDatabaseService<T> databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    protected virtual Task<ActionResult<List<T>>> OnBeforeGetAllAsync() => Task.FromResult<ActionResult<List<T>>>(null);
    protected virtual Task<ActionResult<T>> OnBeforeGetAsync(T entity) => Task.FromResult<ActionResult<T>>(null);
    protected virtual Task<ActionResult<T>> OnBeforePutAsync(T entity) => Task.FromResult<ActionResult<T>>(null);
    protected virtual Task<ActionResult<T>> OnBeforePostAsync(T entity) => Task.FromResult<ActionResult<T>>(null);
    protected virtual Task<ActionResult<T>> OnAfterPostAsync(T entity) => Task.FromResult<ActionResult<T>>(null);
    protected virtual Task<ActionResult<T>> OnBeforeDeleteAsync(T entity) => Task.FromResult<ActionResult<T>>(null);

    [HttpGet]
    public async Task<ActionResult<List<T>>> GetEntities()
    {
        var pre = await OnBeforeGetAllAsync();
        if (pre is not null)
            return pre;

        return await _databaseService.GetAsync();
    }


    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<T>> GetEntity(string id)
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
    public async Task<ActionResult<T>> PostEntity(T entity)
    {
        var pre = await OnBeforePostAsync(entity);
        if (pre is not null)
            return pre;

        await _databaseService.CreateAsync(entity);
        return CreatedAtAction(nameof(GetEntity), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<T>> UpdateEntity(string id, T updatedEntity)
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
        var properties = typeof(T).GetProperties();
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
    public async Task<ActionResult<T>> DeleteEntity(string id)
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