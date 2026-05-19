using ExcelBotCs.Attributes;
using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
[MemberAuth]
public class AuthController : AuthorizedController
{
    private readonly ICurrentMemberAccessor _currentMemberAccessor;

    public AuthController(ILogger<AuthController> logger, ICurrentMemberAccessor currentMemberAccessor) : base(logger)
    {
        _currentMemberAccessor = currentMemberAccessor;
    }

    [HttpHead]
    public IActionResult Index()
    {
        return Ok();
    }

    [HttpGet]
    [Route("me")]
    public async Task<ActionResult<MemberResponse>> GetMe()
    {
        var member = await _currentMemberAccessor.GetCurrentAsync();
        if (member is not null)
        {
            return member.ToDto();
        }

        return Unauthorized();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}