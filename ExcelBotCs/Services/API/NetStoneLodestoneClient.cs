using ExcelBotCs.Services.API.Interfaces;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.FreeCompany;
using NetStone.Model.Parseables.FreeCompany.Members;

namespace ExcelBotCs.Services.API;

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

    public Task<LodestoneCharacter?> GetCharacter(string id)
    {
        return _inner.GetCharacter(id);
    }

    public Task<LodestoneFreeCompany?> GetFreeCompany(string id)
    {
        return _inner.GetFreeCompany(id);
    }

    public Task<FreeCompanyMembers?> GetFreeCompanyMembers(string id)
    {
        return _inner.GetFreeCompanyMembers(id);
    }
}