namespace ExcelBotCs.Models.LodestoneClient;

public class FreeCompanyReputation
{
    public FreeCompanyReputationEntry Maelstrom { get; set; }
    public FreeCompanyReputationEntry Adders { get; set; }
    public FreeCompanyReputationEntry Flames { get; set; }

    public FreeCompanyReputation()
    {
        Maelstrom = new FreeCompanyReputationEntry();
        Adders = new FreeCompanyReputationEntry();
        Flames = new FreeCompanyReputationEntry();
    }

    public FreeCompanyReputation(NetStone.Model.Parseables.FreeCompany.FreeCompanyReputation reputation)
    {
        Maelstrom = new FreeCompanyReputationEntry(reputation.Maelstrom);
        Adders = new FreeCompanyReputationEntry(reputation.Adders);
        Flames = new FreeCompanyReputationEntry(reputation.Flames);
    }

    public FreeCompanyReputationEntry GrandCompanyRep(GrandCompany gc)
    {
        return gc switch
        {
            GrandCompany.Maelstrom => Maelstrom,
            GrandCompany.OrderOfTheTwinAdder => Adders,
            GrandCompany.ImmortalFlames => Flames,
            _ => throw new ArgumentException("Unknown Grand Company")
        };
    }
}