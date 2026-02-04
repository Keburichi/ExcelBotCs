namespace ExcelBotCs.Models.LodestoneClient;

public class FreeCompanyEstate
{
    public string Name { get; set; }
    public string Greeting { get; set; }
    public string Plot { get; set; }
    public bool Exists { get; set; }

    public FreeCompanyEstate()
    {
        Name = string.Empty;
        Greeting = string.Empty;
        Plot = string.Empty;
        Exists = false;
    }

    public FreeCompanyEstate(NetStone.Model.Parseables.FreeCompany.FreeCompanyEstate lodestoneEstate)
    {
        Name = lodestoneEstate.Name;
        Greeting = lodestoneEstate.Greeting;
        Plot = lodestoneEstate.Plot;
        Exists = lodestoneEstate.Exists;
    }
}