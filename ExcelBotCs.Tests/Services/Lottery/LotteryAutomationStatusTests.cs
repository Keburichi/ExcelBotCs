using ExcelBotCs.Services.Lottery.Records;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The Live / Dry run / Paused / Disabled status (spec §7). The scheduler and the admin page both read it, so the
///     precedence between the flags is pinned here: the deployment kill switch wins over the admin pause, which wins
///     over the dry-run flag.
/// </summary>
public class LotteryAutomationStatusTests
{
	[Theory]
	// enabled, dryRun, pauseAutomation, kind, label, scheduler runs
	[InlineData(true, false, false, "live", "Live", true)]
	[InlineData(true, true, false, "dry-run", "Dry run", true)]
	[InlineData(true, false, true, "paused", "Paused", false)]
	[InlineData(true, true, true, "paused", "Paused", false)]
	[InlineData(false, false, false, "disabled", "Disabled", false)]
	[InlineData(false, true, false, "disabled", "Disabled", false)]
	[InlineData(false, false, true, "disabled", "Disabled", false)]
	[InlineData(false, true, true, "disabled", "Disabled", false)]
	public void Derive_FollowsTheDocumentedPrecedence(bool enabled, bool dryRun, bool pauseAutomation, string kind,
		string label, bool schedulerEnabled)
	{
		var status = LotteryAutomationStatus.Derive(enabled, dryRun, pauseAutomation);

		status.Kind.ShouldBe(kind);
		status.Label.ShouldBe(label);
		status.SchedulerEnabled.ShouldBe(schedulerEnabled);
	}

	[Fact]
	public void DisabledIsTheFinalOverride()
	{
		// A paused, dry-run deployment that is switched off is Disabled, not Paused or Dry run.
		LotteryAutomationStatus.Derive(false, true, true).ShouldBe(LotteryAutomationStatus.Disabled);
	}

	[Fact]
	public void PausedOverridesDryRun()
	{
		LotteryAutomationStatus.Derive(true, true, true).ShouldBe(LotteryAutomationStatus.Paused);
	}

	[Fact]
	public void TheStatusLabelsMatchTheSpec()
	{
		LotteryAutomationStatus.Live.Label.ShouldBe("Live");
		LotteryAutomationStatus.DryRun.Label.ShouldBe("Dry run");
		LotteryAutomationStatus.Paused.Label.ShouldBe("Paused");
		LotteryAutomationStatus.Disabled.Label.ShouldBe("Disabled");
	}
}
