namespace ExcelBotCs.Models.LodestoneClient;

public class LodestoneFreeCompany
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Slogan { get; set; }
    public string Tag { get; set; }
    public IconLayers CrestLayers { get; set; }
    public DateTime Formed { get; set; }
    public string GrandCompany { get; set; }
    public int Rank { get; set; }
    public int? RankingMonthly { get; set; }
    public int? RankingWeekly { get; set; }
    public string Recuitment { get; set; }
    public int ActiveMemberCount { get; set; }
    public string ActiveState { get; set; }
    public FreeCompanyEstate? Estate { get; set; }
    public FreeCompanyFocus? Focus { get; set; }
    public FreeCompanyReputation Reputation { get; set; }
    public string World { get; set; }

    public LodestoneFreeCompany()
    {
        Name = string.Empty;
        Id = string.Empty;
        Slogan = string.Empty;
        Tag = string.Empty;
        CrestLayers = new IconLayers();
        Formed = DateTime.Now;
        GrandCompany = string.Empty;
        Rank = 0;
        RankingMonthly = 0;
        RankingWeekly = 0;
        Recuitment = string.Empty;
        ActiveMemberCount = 0;
        ActiveState = string.Empty;
        Estate = null;
        Focus = null;
        Reputation = new FreeCompanyReputation();
        World = string.Empty;
    }

    public LodestoneFreeCompany(NetStone.Model.Parseables.FreeCompany.LodestoneFreeCompany lodestoneFreeCompany)
    {
        Name = lodestoneFreeCompany.Name;
        Id = string.Empty;
        Slogan = string.Empty;
        Tag = string.Empty;
        CrestLayers = new IconLayers();
        Formed = DateTime.Now;
        GrandCompany = string.Empty;
        Rank = 0;
        RankingMonthly = 0;
        RankingWeekly = 0;
        Recuitment = string.Empty;
        ActiveMemberCount = 0;
        ActiveState = string.Empty;
        Estate = lodestoneFreeCompany.Estate == null ? null : new FreeCompanyEstate(lodestoneFreeCompany.Estate);
        Focus = lodestoneFreeCompany.Focus == null ? null : new FreeCompanyFocus(lodestoneFreeCompany.Focus);
        Reputation = new FreeCompanyReputation(lodestoneFreeCompany.Reputation);
        World = string.Empty;
    }
}