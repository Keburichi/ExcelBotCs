namespace ExcelBotCs.Models.Database;

public class FcMember : BaseEntity
{
    public string Name { get; set; }
    public string CharacterId { get; set; }
    public string Title { get; set; }
    public DateTime LastSynchronisation { get; set; }
    public string FcRank { get; set; }
    public string Avatar { get; set; }
    public string Bio { get; set; }

    public FcRankEnum Rank
    {
        get
        {
            // Map the string FCRank to the enum for sorting
            switch (FcRank)
            {
                case "Master":
                    return FcRankEnum.Master;
                case "Officer":
                    return FcRankEnum.Officer;
                case "Living Memory":
                    return FcRankEnum.LivingMemory;
                case "Member":
                default:
                    return FcRankEnum.Member;
            }
        }
    }

    public override string ToString()
    {
        return $"{Name} - {CharacterId}";
    }

    public enum FcRankEnum
    {
        Master,
        Officer,
        Member,
        LivingMemory
    }
}