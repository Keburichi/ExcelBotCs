using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[Route("api/[controller]")]
public class AuthController : AuthorizedController
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly IMemberService _memberService;

    public AuthController(ILogger<AuthController> logger, ICurrentMemberAccessor currentMemberAccessor,
        IMemberService memberService) : base(logger)
    {
        _currentMemberAccessor = currentMemberAccessor;
        _memberService = memberService;
    }

    [HttpHead]
    public IActionResult Index()
    {
        return Ok();
    }

    [HttpGet]
    [Route("me")]
    public async Task<ActionResult<Member>> GetMe()
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is not null)
        {
            // // Assign debug roles
            // member.Roles = new List<MemberRole>()
            // {
            //     new MemberRole()
            //     {
            //         Name = "Debug",
            //         IsAdmin = true,
            //         IsMember = true,
            //         CreateDate = DateTime.UtcNow,
            //         EditDate = DateTime.UtcNow
            //     }
            // };
            //
            // await _memberService.UpdateAsync(member.Id, member);

            return member;
        }

        return Unauthorized();
    }
}