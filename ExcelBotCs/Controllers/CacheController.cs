using ExcelBotCs.Attributes;
using ExcelBotCs.Caching;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Models.DTO.Cache;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[AdminAuth]
[Route("api/[controller]")]
public class CacheController : AuthorizedController
{
    private readonly IEntityCacheService _cacheService;

    public CacheController(
        ILogger<CacheController> logger,
        IEntityCacheService cacheService) : base(logger)
    {
        _cacheService = cacheService;
    }

    [HttpGet("status")]
    public ActionResult<CacheStatusResponse> GetStatus()
    {
        return Ok(_cacheService.GetStatus());
    }

    [HttpGet("{entityType}")]
    public ActionResult GetCachedEntities(string entityType)
    {
        if (!_cacheService.EntityTypes.Contains(entityType))
            return BadRequest($"Unknown entity type: {entityType}");

        return Ok(_cacheService.GetAllEntities(entityType));
    }

    [HttpPost("{entityType}/clear")]
    public async Task<ActionResult> ClearCache(string entityType)
    {
        if (!_cacheService.EntityTypes.Contains(entityType))
            return BadRequest($"Unknown entity type: {entityType}");

        await _cacheService.ClearAsync(entityType);
        return NoContent();
    }

    [HttpPost("{entityType}/fill")]
    public async Task<ActionResult> FillCache(string entityType)
    {
        if (!_cacheService.EntityTypes.Contains(entityType))
            return BadRequest($"Unknown entity type: {entityType}");

        await _cacheService.FillAsync(entityType);
        return NoContent();
    }

    [HttpPost("clear-all")]
    public async Task<ActionResult> ClearAll()
    {
        await _cacheService.ClearAllAsync();
        return NoContent();
    }

    [HttpPost("fill-all")]
    public async Task<ActionResult> FillAll()
    {
        await _cacheService.FillAllAsync();
        return NoContent();
    }
}
