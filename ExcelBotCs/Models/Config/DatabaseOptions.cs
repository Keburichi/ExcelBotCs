
using System.ComponentModel.DataAnnotations;
using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Database")]
public class DatabaseOptions
{
	[Required, MinLength(1)]
	public string ConnectionString { get; set; }
	
	[Required, MinLength(1)]
	public string DatabaseName { get; set; }
}