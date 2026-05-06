namespace ExcelBotCs.Models.LodestoneClient;

public class FreeCompanyReputationEntry
{
    public string Name { get; set; }
    public int Progress { get; set; }
    public string Rank { get; set; }

    public FreeCompanyReputationEntry()
    {
        Name = string.Empty;
        Progress = 0;
        Rank = string.Empty;
    }

    public FreeCompanyReputationEntry(NetStone.Model.Parseables.FreeCompany.FreeCompanyReputationEntry reputationEntry)
    {
        Name = reputationEntry.Name;
        Progress = reputationEntry.Progress;
        Rank = reputationEntry.Rank;
    }
}