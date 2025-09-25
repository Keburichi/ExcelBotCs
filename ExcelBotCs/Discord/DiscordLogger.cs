using Discord;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace ExcelBotCs.Discord;

public class DiscordLogger : TextWriter
{
	private readonly DiscordBotService _discord;
	private readonly TextWriter _stdOut;
	private readonly ConcurrentQueue<string> _logQueue;
	private ITextChannel? _channel;

	private const ulong LogChannel = 1275042232797237279;

	public DiscordLogger(DiscordBotService discord)
	{
		_logQueue = new ConcurrentQueue<string>();
		_discord = discord;
		_stdOut = Console.Out;
		Console.SetOut(this);

		Task.Run(FlushLog);
	}

	private async void FlushLog()
	{
		while (true)
		{
			if (_logQueue.TryDequeue(out var line))
			{
				if (_channel == null)
				{
					try
					{
						var channel = await _discord.Client.GetChannelAsync(LogChannel);
						if (channel is not ITextChannel textChannel)
							return;

						_channel = textChannel;
					}
					catch (Exception e)
					{
						Debug.WriteLine($"Exception: {e} {Environment.NewLine}Unable to access log channel");
					}
				}

				if(_channel != null)
					await _channel.SendMessageAsync(line);
			}

			await Task.Delay(TimeSpan.FromSeconds(0.5));
		}
	}

	public override void WriteLine(string? line)
	{
		if (line.Contains($"POST channels/{LogChannel}/messages"))
			return;

		if (line.Contains("Received Dispatch (MESSAGE_CREATE)"))
			return;

		_stdOut.WriteLine(line);
		Debug.WriteLine(line);
		_logQueue.Enqueue(line);
	}

	public override Encoding Encoding { get; }
}