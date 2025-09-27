using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FcMembersController : BaseCrudController<FcMember>
{
    public FcMembersController(ILogger<FcMembersController> logger, FcMemberService fcMemberService) : base(logger,
        fcMemberService)
    {
    }
}