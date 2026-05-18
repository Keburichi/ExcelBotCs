using ExcelBotCs.Models.DTO.Events;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers.Interfaces;

public interface IEventTemplatesController
{
    // CRUD operations
    public Task<ActionResult<List<EventTemplateResponse>>> GetTemplates();
    public Task<ActionResult<EventTemplateResponse>> GetTemplate(string id);
    public Task<ActionResult<EventTemplateResponse>> CreateTemplate(CreateEventTemplateRequest request);
    public Task<ActionResult<EventTemplateResponse>> UpdateTemplate(string id, UpdateEventTemplateRequest request);
    public Task<ActionResult> DeleteTemplate(string id);
}