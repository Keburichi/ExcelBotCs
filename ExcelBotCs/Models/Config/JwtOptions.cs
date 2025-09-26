using System.ComponentModel.DataAnnotations;
using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.Config;

[OptionsSection("Jwt")]
public class JwtOptions
{
    [Required]
    public required string RsaPrivateKeyLocation { get; set; }
    
    [Required]
    public required string RsaPublicKeyLocation { get; set; }
    
    [Required]
    public required string Issuer { get; set; }
    
    [Required]
    public required string Audience { get; set; }
}