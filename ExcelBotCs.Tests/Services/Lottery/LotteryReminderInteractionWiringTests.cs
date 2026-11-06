using System.Reflection;
using Discord.Interactions;
using ExcelBotCs.Modules.Lottery;

namespace ExcelBotCs.Tests.Services.Lottery;

/// <summary>
///     The Discord wiring for the reminder (spec §5.7, §10): the two commands and the opt-out component must be declared
///     exactly as the accepted copy and the posted button describe, or the interaction service never routes them.
/// </summary>
public class LotteryReminderInteractionWiringTests
{
	private static MethodInfo DeclaredMethod(string name)
	{
		return typeof(LotteryInteraction).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
		       ?? throw new InvalidOperationException($"{nameof(LotteryInteraction)}.{name} is missing.");
	}

	[Fact]
	public void TheRemindersCommand_IsDeclaredAsLotteryReminders()
	{
		var attribute = DeclaredMethod(nameof(LotteryInteraction.Reminders))
			.GetCustomAttribute<SlashCommandAttribute>();

		attribute.ShouldNotBeNull();
		attribute!.Name.ShouldBe("reminders");
	}

	[Fact]
	public void TheOldRemindCommand_IsStillDeclared()
	{
		var attribute = DeclaredMethod(nameof(LotteryInteraction.Remind))
			.GetCustomAttribute<SlashCommandAttribute>();

		attribute.ShouldNotBeNull();
		attribute!.Name.ShouldBe("remind");
	}

	[Fact]
	public void TheOptOutButton_IsRoutedToTheHandler()
	{
		var handler = typeof(LotteryInteraction).GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.SingleOrDefault(method => method.GetCustomAttribute<ComponentInteractionAttribute>() is not null
			                           && method.GetCustomAttribute<ComponentInteractionAttribute>()!.CustomId
			                           == LotteryReminderComponents.OptOutCustomId);

		handler.ShouldNotBeNull();
		handler!.Name.ShouldBe(nameof(LotteryInteraction.TurnOffReminders));
		handler.GetParameters().ShouldBeEmpty("the opt-out handler takes no component arguments");
	}
}
