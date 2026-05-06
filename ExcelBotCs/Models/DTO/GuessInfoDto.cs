using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Models.DTO;

public record GuessInfoDto
{
    public int Number { get; set; }
    public List<LotteryUser> Guessers { get; set; }
}