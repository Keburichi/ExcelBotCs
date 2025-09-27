using ExcelBotCs.Database.DTO;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using NetStone;
using NetStone.Model.Parseables.FreeCompany.Members;

namespace ExcelBotCs.Services;

public class LodestoneService
{
    private LodestoneClient _lodestoneClient;
    private readonly IOptions<LodestoneOptions> _options;
    private readonly FcMemberService _fcMemberService;

    public LodestoneService(IOptions<LodestoneOptions> options, FcMemberService fcMemberService)
    {
        _options = options;
        _fcMemberService = fcMemberService;
        Task.Run(InitializeClientAsync);
    }

    private async Task InitializeClientAsync()
    {
        try
        {
            _lodestoneClient = await LodestoneClient.GetClientAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<FreeCompanyMembersEntry>> ImportMembers()
    {
        if (_lodestoneClient == null)
            return new List<FreeCompanyMembersEntry>();
        
        var fc = await _lodestoneClient.GetFreeCompany(_options.Value.FCId);
        var members = await fc.GetMembers();
        
        var fcMembers = new List<FreeCompanyMembersEntry>();

        while (members != null && members.CurrentPage <= members.NumPages)
        {
            Console.WriteLine("Adding members");
            fcMembers.AddRange(members.Members);
            members = await members.GetNextPage();
        }
        
        // Import all fc members into the database or update existing entries
        foreach (var freeCompanyMembersEntry in fcMembers)
        {
            var dbFcMember = await _fcMemberService.GetByCharacterId(freeCompanyMembersEntry.Id);

            if (dbFcMember == null)
            {
                await _fcMemberService.CreateAsync(await CreateFcMember(freeCompanyMembersEntry));
            }
            else
            {
                await _fcMemberService.UpdateAsync(dbFcMember.Id, await CreateFcMember(freeCompanyMembersEntry));
            }
        }
        
        return fcMembers;
    }

    private async Task<FcMember> CreateFcMember(FreeCompanyMembersEntry freeCompanyMembersEntry)
    {
        var lodestoneCharacter = await _lodestoneClient.GetCharacter(freeCompanyMembersEntry.Id);
        
        return new FcMember()
        {
            CharacterId = freeCompanyMembersEntry.Id,
            Avatar = freeCompanyMembersEntry.Avatar?.ToString() ?? string.Empty,
            LastSynchronisation = DateTime.UtcNow,
            FcRank = freeCompanyMembersEntry.FreeCompanyRank,
            Name = freeCompanyMembersEntry.Name,
            Title = lodestoneCharacter?.Title ??  string.Empty,
            Bio = lodestoneCharacter.Bio
        };
    }
}