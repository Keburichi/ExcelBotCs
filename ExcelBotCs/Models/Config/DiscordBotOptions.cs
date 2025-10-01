using System.ComponentModel.DataAnnotations;
using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("DiscordBot")]
public class DiscordBotOptions
{
	[Required, MinLength(1)]
	public string Token { get; set; } = string.Empty;
	
	[Required]
	public ulong GuildId { get; set; }
	
	[Required]
	public ulong LotteryChannel { get; set; }
	
	[Required]
	public ulong AnnouncementChannel { get; set; }
	
	[Required]
	public ulong EventsChannel { get; set; }
	
	[Required]
	public ulong UpcomingRosterChannel { get; set; }
}