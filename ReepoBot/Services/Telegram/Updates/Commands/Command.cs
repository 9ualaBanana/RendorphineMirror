using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public abstract class Command
{
    const char Prefix = '/';

    protected readonly ILogger Logger;
    protected readonly TelegramBot Bot;
    protected readonly TelegramChatIdAuthentication Authentication;

    public Command(ILogger<Command> logger, TelegramBot bot, TelegramChatIdAuthentication authentication)
    {
        Logger = logger;
        Bot = bot;
        Authentication = authentication;
    }

    public bool Matches(string command) => command.StartsWith(Prefix + Value);
    public abstract string Value { get; }
    internal abstract Task HandleAsync(Update update);
}
