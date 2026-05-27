namespace ExcelBotCs.Models.DTO.Cache;

public class CacheStatusResponse
{
    public List<CacheEntityStatus> Entities { get; set; } = [];
}

public class CacheEntityStatus
{
    public string EntityType { get; set; } = "";
    public int Count { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public DateTime? MaxDateModified { get; set; }
    public bool IsPopulated { get; set; }
}
