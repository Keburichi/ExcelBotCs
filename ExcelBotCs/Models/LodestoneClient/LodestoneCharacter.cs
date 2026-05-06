namespace ExcelBotCs.Models.LodestoneClient;

public class LodestoneCharacter
{
    public string ActiveClassJobIcon { get; set; }
    public int ActiveClassJobLevel { get; set; }
    public Uri? Avatar { get; set; }
    public string Bio { get; set; }
    public string GrandCompanyName { get; set; }
    public string GrandCompanyRank { get; set; }
    public string GuardianDeityName { get; set; }
    public Uri? GuardianDeityIcon { get; set; }
    public string Name { get; set; }
    public string Nameday { get; set; }
    public Uri? Portrait { get; set; }
    public string Server { get; set; }
    public string Title { get; set; }
    public string TownName { get; set; }

    public LodestoneCharacter()
    {
    }

    public LodestoneCharacter(NetStone.Model.Parseables.Character.LodestoneCharacter character)
    {
        ActiveClassJobIcon = character.ActiveClassJobIcon;
        ActiveClassJobLevel = character.ActiveClassJobLevel;
        Avatar = character.Avatar;
        Bio = character.Bio;
        GrandCompanyName = character.GrandCompanyName;
        GrandCompanyRank = character.GrandCompanyRank;
        GuardianDeityName = character.GuardianDeityName;
        GuardianDeityIcon = character.GuardianDeityIcon;
        Name = character.Name;
        Nameday = character.Nameday;
        Portrait = character.Portrait;
        Server = character.Server;
        Title = character.Title;
        TownName = character.TownName;
    }

    public override string ToString()
    {
        return $"{Name} on {Server}";
    }
}