using System.ComponentModel.DataAnnotations;
using ExcelBotCs.Models.Config;

namespace ExcelBotCs.Tests.Models.Config;

public class DiscordBotOptionsTests
{
    private static List<ValidationResult> Validate(DiscordBotOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, true);
        return results;
    }

    [Fact]
    public void Defaults_LeaveTheBotEnabledAndRegisteringCommands()
    {
        var options = new DiscordBotOptions();

        options.Enabled.ShouldBeTrue();
        options.RegisterCommands.ShouldBeTrue();
    }

    [Fact]
    public void Disabled_WithoutAToken_IsValid()
    {
        var options = new DiscordBotOptions { Enabled = false, Token = string.Empty };

        Validate(options).ShouldBeEmpty();
    }

    [Fact]
    public void Enabled_WithoutAToken_ReportsTheTokenAsRequired()
    {
        var options = new DiscordBotOptions { Enabled = true, Token = string.Empty };

        var results = Validate(options);

        results.Count.ShouldBe(1);
        results[0].MemberNames.ShouldContain(nameof(DiscordBotOptions.Token));
    }

    [Fact]
    public void Enabled_WithAToken_IsValid()
    {
        var options = new DiscordBotOptions { Enabled = true, Token = "not-a-real-token" };

        Validate(options).ShouldBeEmpty();
    }

    [Fact]
    public void RegisterCommands_CanBeDisabledWhileTheBotStaysEnabled()
    {
        var options = new DiscordBotOptions { Enabled = true, RegisterCommands = false, Token = "not-a-real-token" };

        Validate(options).ShouldBeEmpty();
        options.RegisterCommands.ShouldBeFalse();
    }
}
