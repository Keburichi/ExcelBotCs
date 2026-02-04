namespace ExcelBotCs.Models.LodestoneClient;

public class FreeCompanyFocusEntry
{
    public string Name { get; set; }
    public Uri? Icon { get; set; }
    public bool IsEnabled { get; set; }

    public FreeCompanyFocusEntry()
    {
        Name = string.Empty;
        Icon = null;
        IsEnabled = false;
    }

    public FreeCompanyFocusEntry(NetStone.Model.Parseables.FreeCompany.FreeCompanyFocusEntry focusEntry)
    {
        Name = focusEntry.Name;
        Icon = focusEntry.Icon;
        IsEnabled = focusEntry.IsEnabled;
    }
}