using ExcelBotCs.Database.DTO;
using ExcelBotCs.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberRolesController : BaseCrudController<MemberRole>
{
    private readonly MemberService _memberService;

    public MemberRolesController(ILogger<MemberRolesController> logger, MemberRoleService memberRoleService,
        MemberService memberService) : base(logger,
        memberRoleService)
    {
        _memberService = memberService;
    }

    protected override async Task<ActionResult<MemberRole>> OnAfterPostAsync(MemberRole entity)
    {
        // Update the role of each individual user
        var members = await _memberService.GetAsync();

        foreach (var member in members.Where(x => x.Roles.Any(x => x.Id == entity.Id)))
        {
            var memberRole = member.Roles.First(x => x.Id == entity.Id);
            memberRole.IsAdmin = entity.IsAdmin;
            memberRole.IsMember = entity.IsMember;
            memberRole.DiscordId = entity.DiscordId;
            memberRole.EditDate = entity.EditDate;
            
            await _memberService.UpdateAsync(member.Id, member);
        }

        return null;
    }
}