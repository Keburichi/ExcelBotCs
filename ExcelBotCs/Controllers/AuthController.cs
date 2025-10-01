using ExcelBotCs.Models.Database;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[Route("api/[controller]")]
public class AuthController : AuthorizedController
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;
    private readonly MemberService _memberService;

    public AuthController(ICurrentMemberAccessor currentMemberAccessor, MemberService memberService)
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