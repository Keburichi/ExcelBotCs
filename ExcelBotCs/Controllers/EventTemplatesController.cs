using ExcelBotCs.Attributes;
using ExcelBotCs.Controllers.Interfaces;
using ExcelBotCs.Mappers.Events;
using ExcelBotCs.Models.DTO.Events;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[AdminAuth]
[Route("api/event-templates")]
public class EventTemplatesController : AuthorizedController, IEventTemplatesController
{
    private readonly IEventTemplateService _templateService;

    public EventTemplatesController(ILogger<EventTemplatesController> logger, IEventTemplateService templateService)
        : base(logger)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<ActionResult<List<EventTemplateResponse>>> GetTemplates()
    {
        var templates = await _templateService.GetAsync();
        return Ok(templates.ToResponse());
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<EventTemplateResponse>> GetTemplate(string id)
    {
        var template = await _templateService.GetAsync(id);
        if (template is null)
            return NotFound();

        return Ok(template.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<EventTemplateResponse>> CreateTemplate(CreateEventTemplateRequest request)
    {
        var entity = request.ToEntity();
        await _templateService.CreateAsync(entity);
        return CreatedAtAction(nameof(GetTemplate), new { id = entity.Id }, entity.ToResponse());
    }

    [HttpPut("{id:length(24)}")]
    public async Task<ActionResult<EventTemplateResponse>> UpdateTemplate(string id,
        [FromBody] UpdateEventTemplateRequest request)
    {
        var existing = await _templateService.GetAsync(id);
        if (existing is null)
            return NotFound();

        existing.ApplyUpdate(request);
        await _templateService.UpdateAsync(id, existing);

        return Ok(existing.ToResponse());
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<ActionResult> DeleteTemplate(string id)
    {
        var existing = await _templateService.GetAsync(id);
        if (existing is null)
            return NotFound();

        await _templateService.DeleteAsync(id);
        return NoContent();
    }
}