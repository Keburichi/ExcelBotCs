using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : BaseCrudController<Member>
{
    private readonly MemberService _memberService;

    public MembersController(ILogger<MembersController> logger, MemberService memberService) : base(logger,
        memberService)
    {
        _memberService = memberService;
    }
}