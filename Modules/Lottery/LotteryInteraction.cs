using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ExcelBotCs.Commands;
using ExcelBotCs.Data;
using ExcelBotCs.Discord;
using MongoDB.Driver;

namespace ExcelBotCs.Modules.Lottery;

[Group("lottery", "Lottery commands")]
public class LotteryInteraction : InteractionModuleBase<SocketInteractionContext>
{
	private readonly Repository<ExtraLotteryGuess> _extraLotteryGuesses;
	private readonly Repository<LotteryGuess> _lotteryGuesses;

	public LotteryInteraction(DiscordBotService discord, Database database)
	{
		_extraLotteryGuesses = database.GetCollection<ExtraLotteryGuess>("extra_lottery_guesses");
		_lotteryGuesses = database.GetCollection<LotteryGuess>("lottery_guesses");
	}

	private bool CanParticipate(SocketGuildUser user)
	{
		return user.Roles.IsMember();
	}

	interface IGuessResponse
	{
	}
	record SuccessGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses) : IGuessResponse;
	record OutOfRangeGuessResponse : IGuessResponse;
	record NotFcMemberGuessResponse : IGuessResponse;
	record AlreadyGuessedNumberGuessResponse(int Number) : IGuessResponse;
	record NotCurrentGuessedNumberGuessResponse(int Number) : IGuessResponse;
	record NoMoreGuessesGuessResponse(IEnumerable<int> CurrentGuesses, string PrettyCurrentGuesses) : IGuessResponse;

	private async Task<IGuessResponse> TryGuess(SocketGuildUser user, int number)
	{
		if (!CanParticipate(Context.GuildUser()))
			return new NotFcMemberGuessResponse();

		if (number is <= 0 or >= 100)
			return new OutOfRangeGuessResponse();

		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		if (currentGuesses.Any(guess => guess.Number == number))
			return new AlreadyGuessedNumberGuessResponse(number);

		var extraAwardedGuesses = await _extraLotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		var currentGuessAmount = currentGuesses.Count;
		var allowedGuessAmount = 1 + extraAwardedGuesses.Count;

		var numbers = currentGuesses.Select(guess => guess.Number).ToList();
		var guessesBlocked = allowedGuessAmount - currentGuessAmount <= 0;

		if (!guessesBlocked)
			numbers.Add(number);

		numbers.Sort();
		var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

		return guessesBlocked
			? new NoMoreGuessesGuessResponse(numbers, prettyNumbers)
			: new SuccessGuessResponse(numbers, prettyNumbers);
	}

	private async Task<IGuessResponse> TryChangeGuess(SocketGuildUser user, int oldNumber, int newNumber)
	{
		if (!CanParticipate(Context.GuildUser()))
			return new NotFcMemberGuessResponse();

		if (newNumber is <= 0 or >= 100)
			return new OutOfRangeGuessResponse();

		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		if (currentGuesses.All(guess => guess.Number != oldNumber))
			return new NotCurrentGuessedNumberGuessResponse(oldNumber);

		if (currentGuesses.Any(guess => guess.Number == newNumber))
			return new AlreadyGuessedNumberGuessResponse(newNumber);

		currentGuesses.RemoveAll(guess => guess.Number == oldNumber);

		var numbers = ((List<int>)[newNumber, .. currentGuesses.Select(guess => guess.Number)]).ToList();
		numbers.Sort();
		var prettyNumbers = numbers.Select(guess => guess.ToString()).ToList().PrettyJoin();

		return new SuccessGuessResponse(numbers, prettyNumbers);
	}

	private async Task<string> ChangeGuess(SuccessGuessResponse success, int oldNumber, int newNumber)
	{
		await _lotteryGuesses.Delete(guess => guess.Number == oldNumber);
		await _lotteryGuesses.Insert(new LotteryGuess() { DiscordId = Context.GuildUser().Id, Number = newNumber });
		return $"Your guess for {oldNumber} was changed to {newNumber}! Current guesses: {success.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.";
	}

	private async Task<string> InsertGuess(SuccessGuessResponse success, int number)
	{
		await _lotteryGuesses.Insert(new LotteryGuess() { DiscordId = Context.GuildUser().Id, Number = number });
		return $"Your guess for {number} was recorded! Current guesses: {success.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.";
	}

	[SlashCommand("guess", "Pick a number and have a chance to win!")]
	public async Task Guess(int number)
	{
		var result = await TryGuess(Context.GuildUser(), number);

		await RespondAsync(result switch
		{
			NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
			OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
			AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {number}!",
			NoMoreGuessesGuessResponse r => $"You don't have any guesses left! Current guesses: {r.PrettyCurrentGuesses}. You can use `/lottery change` to change an existing guess.",
			SuccessGuessResponse r => await InsertGuess(r, number),
			_ => throw new NotImplementedException()
		}, ephemeral: true);

		if (result is SuccessGuessResponse success)
			await PostInLotteryChannel($"<@{Context.GuildUser().Id}> guessed {number}. Current guesses: {success.PrettyCurrentGuesses}");
	}

	[SlashCommand("change", "Change one of your current guesses")]
	public async Task Change(int old, int @new)
	{
		var result = await TryChangeGuess(Context.GuildUser(), old, @new);

		await RespondAsync(result switch
		{
			NotFcMemberGuessResponse _ => "Only FC members can participate in the lottery",
			OutOfRangeGuessResponse _ => "You can only pick a number between 1 and 99.",
			AlreadyGuessedNumberGuessResponse _ => $"You have already guessed {@new}.",
			NotCurrentGuessedNumberGuessResponse _ => $"You have not guessed {old}. You need to use a number you have already guessed in order to change it.",
			SuccessGuessResponse r => await ChangeGuess(r, old, @new),
			_ => throw new NotImplementedException()
		}, ephemeral: true);

		if (result is SuccessGuessResponse success)
			await PostInLotteryChannel($"<@{Context.GuildUser().Id}> changed a guess from {old} to {@new}. Current guesses: {success.PrettyCurrentGuesses}");
	}

	[SlashCommand("view", "Check your current guesses and see how many guesses you have left.")]
	public async Task View()
	{
		if (!CanParticipate(Context.GuildUser()))
		{
			await RespondAsync("Only FC members can participate in the lottery", ephemeral: true);
			return;
		}

		var currentGuesses = await _lotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		var extraAwardedGuesses = await _extraLotteryGuesses
			.Where(guess => guess.DiscordId == Context.GuildUser().Id)
			.ToListAsync();

		var currentGuessAmount = currentGuesses.Count;
		var allowedGuessAmount = 1 + extraAwardedGuesses.Count;
		var displayAmount = allowedGuessAmount == 1
			? (currentGuessAmount == 1
				? $"You have used your guess."
				: $"You have not used your guess.")
			: $"You have used {currentGuessAmount} of your {allowedGuessAmount} guesses.";

		if (currentGuesses.Count == 0)
		{
			await RespondAsync(displayAmount, ephemeral: true);
		}
		else
		{
			var guesses = currentGuesses.Select(guess => guess.Number).ToList();
			guesses.Sort();
			var displayedGuesses = guesses.Select(guess => guess.ToString()).ToList().PrettyJoin();

			await RespondAsync($"Current guesses: {displayedGuesses}. {displayAmount}", ephemeral: true);
		}
	}

	[SlashCommand("run", "Runs the lottery.")]
	public async Task Run()
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		await RespondAsync("Lottery running...", ephemeral: true);

		var randomNumber = new Random().Next(99);
		var randomNumberDisplay = $"The winning number is {randomNumber}";

		var winners = await _lotteryGuesses
			.Where(guess => guess.Number == randomNumber)
			.ToListAsync();

		if (winners.Count == 0)
		{
			await PostInLotteryChannel($"{randomNumberDisplay}. Nobody won, better luck next time!");
		}
		else
		{
			var winnersDisplay = winners.Select(user => $"<@{user.DiscordId}>").ToList().PrettyJoin();
			await PostInLotteryChannel($"{randomNumberDisplay}. Congratulations to {winnersDisplay}!");
		}

		await Flush();
	}

	private async Task Flush()
	{
		await _lotteryGuesses.DeleteAll();
		await _extraLotteryGuesses.DeleteAll();
	}

	[SlashCommand("award", "Grants extra guesses for the current lottery period")]
	public async Task Award()
	{
		if (!Context.GuildUser().Roles.IsOfficer())
		{
			await RespondAsync("Only officers can use this command!", ephemeral: true);
			return;
		}

		var awardSelection = new SelectMenuBuilder()
			.WithPlaceholder("Pick users")
			.WithCustomId("award_selection")
			.WithType(ComponentType.UserSelect)
			.WithMinValues(1)
			.WithMaxValues(8);

		var builder = new ComponentBuilder()
			.WithSelectMenu(awardSelection);

		await RespondAsync("Who should be awarded an extra guess for this lottery period?", components: builder.Build(), ephemeral: true);
	}

	[ComponentInteraction("award_selection", ignoreGroupNames: true)]
	public async Task HandleAward(IUser[] users)
	{
		if (Context.Interaction is not SocketMessageComponent test)
		{
			await RespondAsync("Something went wrong. Tell Zahrymm.");
			return;
		}

		if (users.Length == 0)
		{
			await RespondAsync("You did not pick any users.");
			return;
		}

		var text = users.Select(user => $"<@{user.Id}>").ToList().PrettyJoin();

		await test.UpdateAsync(msg =>
		{
			msg.Components = null;
			msg.Content = $"An extra lottery guess have been granted to: {text}";
		});

		foreach (var user in users)
		{
			await _extraLotteryGuesses.Insert(new ExtraLotteryGuess() { DiscordId = user.Id });
		}

		if (users.Length == 1)
		{
			await PostInLotteryChannel($"{text} has been granted another lottery guess! Use `/lottery guess` to make your choice.");
		}
		else
		{
			await PostInLotteryChannel($"{text} have all been granted another lottery guess! Use `/lottery guess` to make your choice.");
		}
	}

	private async Task PostInLotteryChannel(string message)
	{
		var lotteryChannel = Context.Guild.GetTextChannel(Constants.LotteryChannel);
		await lotteryChannel.SendMessageAsync(message);
	}
}