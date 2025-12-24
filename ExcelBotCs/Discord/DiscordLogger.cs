using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Discord;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Services.Discord.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Discord;

public class DiscordLogger : TextWriter
{
    private readonly IDiscordMessageService _discordMessageService;
    private readonly IOptions<DiscordBotOptions> _options;
    private readonly TextWriter _stdOut;
    private readonly ConcurrentQueue<string> _logQueue;
    private ITextChannel? _channel;

    public DiscordLogger(IDiscordMessageService discordMessageService, IOptions<DiscordBotOptions> options)
    {
        _logQueue = new ConcurrentQueue<string>();
        _discordMessageService = discordMessageService;
        _options = options;
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
                        var channel = await _discordMessageService.GetLogChannelAsync();
                        if (channel is null)
                            return;

                        _channel = channel;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Exception: {e} {Environment.NewLine}Unable to access log channel");
                    }
                }

                if (_channel != null)
                    await _channel.SendMessageAsync(line);
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }
    }

    public override void WriteLine(string? line)
    {
        if (line.Contains($"POST channels/{_options.Value.LogChannel}/messages"))
            return;

        if (line.Contains("Received Dispatch (MESSAGE_CREATE)"))
            return;

        _stdOut.WriteLine(line);
        Debug.WriteLine(line);
        _logQueue.Enqueue(line);
    }

    public override Encoding Encoding { get; }
}