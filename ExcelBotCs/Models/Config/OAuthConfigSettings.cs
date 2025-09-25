namespace ExcelBotCs.Models.Config;

public class OAuthConfigSettings
{
    public List<OauthConfigProvider> Providers { get; set; }
}

public class OauthConfigProvider
{
    public string Name { get; set; }
}