namespace ExcelBotCs.Models.LodestoneClient;

public class FreeCompanyFocus
{
    public bool HasFocus { get; set; }
    public bool Exists { get; set; }
    public FreeCompanyFocusEntry RolePlay { get; set; }
    public FreeCompanyFocusEntry Leveling { get; set; }
    public FreeCompanyFocusEntry Casual { get; set; }
    public FreeCompanyFocusEntry Hardcore { get; set; }
    public FreeCompanyFocusEntry Dungeons { get; set; }
    public FreeCompanyFocusEntry Guildhests { get; set; }
    public FreeCompanyFocusEntry Trials { get; set; }
    public FreeCompanyFocusEntry Raids { get; set; }
    public FreeCompanyFocusEntry PvP { get; set; }

    public FreeCompanyFocus()
    {
        HasFocus = false;
        Exists = false;
        RolePlay = new FreeCompanyFocusEntry();
        Leveling = new FreeCompanyFocusEntry();
        Casual = new FreeCompanyFocusEntry();
        Hardcore = new FreeCompanyFocusEntry();
        Dungeons = new FreeCompanyFocusEntry();
        Guildhests = new FreeCompanyFocusEntry();
        Trials = new FreeCompanyFocusEntry();
        Raids = new FreeCompanyFocusEntry();
        PvP = new FreeCompanyFocusEntry();
    }

    public FreeCompanyFocus(NetStone.Model.Parseables.FreeCompany.FreeCompanyFocus focus)
    {
        HasFocus = focus.HasFocus;
        Exists = focus.Exists;
        RolePlay = new FreeCompanyFocusEntry(focus.RolePlay);
        Leveling = new FreeCompanyFocusEntry(focus.Leveling);
        Casual = new FreeCompanyFocusEntry(focus.Casual);
        Hardcore = new FreeCompanyFocusEntry(focus.Hardcore);
        Dungeons = new FreeCompanyFocusEntry(focus.Dungeons);
        Guildhests = new FreeCompanyFocusEntry(focus.Guildhests);
        Trials = new FreeCompanyFocusEntry(focus.Trials);
        Raids = new FreeCompanyFocusEntry(focus.Raids);
        PvP = new FreeCompanyFocusEntry(focus.PvP);
    }
}