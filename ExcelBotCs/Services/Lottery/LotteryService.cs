using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.Lottery;
using ExcelBotCs.Services.Lottery.Interfaces;
using ExcelBotCs.Services.Lottery.Records;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Lottery;

public class LotteryService : ILotteryService
{
    private readonly LotteryOptions _options;
    private readonly Prng _rng;
    private readonly MemberService _memberService;
    private readonly Repository<ExtraLotteryGuess> _extraLotteryGuesses;
    private readonly Repository<LotteryGuess> _lotteryGuesses;
    private readonly Repository<LotteryResult> _lotteryResults;

    public LotteryService(IOptions<DiscordBotOptions> options, Prng rng, Data.Database database, MemberService memberService)
    {
        _options = options.Value.LotteryOptions;
        _rng = rng;
        _memberService = memberService;
        _extraLotteryGuesses = database.GetCollection<ExtraLotteryGuess>("extra_lottery_guesses");
        _lotteryGuesses = database.GetCollection<LotteryGuess>("lottery_guesses");
        _lotteryResults = database.GetCollection<LotteryResult>("lottery_results");
    }


    public async Task<IGuessResponse> Guess(ulong discordUserId, int number)
    {
        var result = await TryGuess(discordUserId, number);

        if (result is SuccessGuessResponse guessResponse)
        {
            await InsertGuess(discordUserId, number);
        }
        
        return result;
    }

    private async Task InsertGuess(ulong discordUserId, int number)
        => await _lotteryGuesses.Insert(new LotteryGuess()
        {
            DiscordId = discordUserId, Number = number
        });

    public async Task<bool> CanParticipate(ulong discordUserId)
    {
        var user = await _memberService.GetByDiscordId(discordUserId);
        return user.IsMember != null && user.IsMember.Value;
    }

    public Task<IGuessResponse> TryGuess(ulong discordUserId, int number)
    {
        throw new NotImplementedException();
    }

    public Task<IGuessResponse> TryChangeGuess(ulong discordUserId, int oldNumber, int newNumber)
    {
        throw new NotImplementedException();
    }

    public Task AwardUsers(SuccessAwardResponse successAwardResponse)
    {
        throw new NotImplementedException();
    }
}