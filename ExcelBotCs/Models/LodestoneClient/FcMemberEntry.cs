using NetStone.Model.Parseables.FreeCompany.Members;

namespace ExcelBotCs.Models.LodestoneClient;

public class FcMemberEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Rank { get; set; }
    public Uri? RankIcon { get; set; }
    public string FreeCompanyRank { get; set; }
    public Uri? FreeCompanyRankIcon { get; set; }
    public string Server { get; set; }
    public string Datacenter { get; set; }
    public Uri? Avatar { get; set; }

    public FcMemberEntry()
    {
    }

    public FcMemberEntry(FreeCompanyMembersEntry entry)
    {
        Id = entry.Id;
        Name = entry.Name;
        Rank = entry.Rank;
        RankIcon = entry.RankIcon;
        FreeCompanyRank = entry.FreeCompanyRank;
        FreeCompanyRankIcon = entry.FreeCompanyRankIcon;
        Server = entry.Server;
        Datacenter = entry.Datacenter;
        Avatar = entry.Avatar;
    }
}