using ExcelBotCs.Models.LodestoneClient;
using ExcelBotCs.Services.API.Interfaces;
using NetStone;

namespace ExcelBotCs.Services.API;

/// <summary>
///     Since the <see cref="LodestoneClient" /> is nearly impossible to properly mock,
///     this class provides some abstraction to hide away the actual fetching of the data
///     and converting everything into model classes that can be mocked.
/// </summary>
public sealed class NetStoneLodestoneClient : ILodestoneClient
{
    private readonly LodestoneClient _inner;

    public NetStoneLodestoneClient(LodestoneClient inner)
    {
        _inner = inner;
    }

    public LodestoneClient GetLodestoneClient()
    {
        return _inner;
    }

    public async Task<LodestoneCharacter?> GetCharacter(string id)
    {
        var character = await _inner.GetCharacter(id);

        if (character is null)
            return null;

        // If a character profile cannot be viewed on lodestone, simply return null and not a garbled object
        try
        {
            var jobLevel = character.ActiveClassJobLevel;
            return new LodestoneCharacter(character);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<LodestoneFreeCompany?> GetFreeCompany(string id)
    {
        var lodestoneFc = await _inner.GetFreeCompany(id);

        return lodestoneFc is null ? null : new LodestoneFreeCompany(lodestoneFc);
    }

    public async Task<List<FcMemberEntry>> GetFreeCompanyMembers(string id)
    {
        var result = await _inner.GetFreeCompanyMembers(id);

        var members = new List<FcMemberEntry>();
        while (result != null && result.CurrentPage <= result.NumPages)
        {
            members.AddRange(
                result.Members.Select(freeCompanyMembersEntry => new FcMemberEntry(freeCompanyMembersEntry)));

            result = await result.GetNextPage();
        }

        return members;
    }
}