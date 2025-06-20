using Discord;
using System.Diagnostics;
using System.Text;

namespace ExcelBotCs.Discord;

public class DiscordLogger : TextWriter
{
	private readonly DiscordBotService _discord;
	private readonly TextWriter _stdOut;
	private ITextChannel? _channel;

	private const ulong LogChannel = 1275042232797237279;

	public DiscordLogger(DiscordBotService discord)
	{
		_discord = discord;
		_stdOut = Console.Out;
		Console.SetOut(this);
	}

	public override async void WriteLine(string? line)
	{
		_stdOut.WriteLine(line);
		Debug.WriteLine(line);

		if (_channel == null)
		{
			var channel = await _discord.Client.GetChannelAsync(LogChannel);
			if (channel is not ITextChannel textChannel)
				return;

			_channel = textChannel;
		}

		await _channel.SendMessageAsync(line);
	}

	public override Encoding Encoding { get; }
}