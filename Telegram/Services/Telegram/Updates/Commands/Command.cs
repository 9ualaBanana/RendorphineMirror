namespace Telegram.Services.Telegram.Updates.Commands;

public abstract class Command : TelegramUpdateHandler
{
    const char Prefix = '/';



    public Command(ILogger<Command> logger, TelegramBot bot) : base(logger, bot)
    {
    }



    public bool Matches(string command) => command.StartsWith(Prefix + Value);
    public abstract string Value { get; }
}
