using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Commands;

namespace Telegram.Telegram.Updates.Commands;

public class TelegramCommandHandler : TelegramUpdateHandler
{
    readonly IEnumerable<Command> _commands;



    public TelegramCommandHandler(ILogger<TelegramMessageHandler> logger, TelegramBot bot, IEnumerable<Command> commands)
        : base(logger, bot)
    {
        _commands = commands;
    }



    public override async Task HandleAsync(Update update)
    {
        var receivedCommandText = CommandTextFrom(update);
        var command = Match(receivedCommandText);
        if (command is not null) await command.HandleAsync(update);
        else Logger.LogDebug("No handler for {Command} command is found", receivedCommandText);
    }

    string CommandTextFrom(Update update)
    {
        var receivedCommandMessage = update.Message!.Text!;
        var command = receivedCommandMessage.Command();
        var arguments = receivedCommandMessage.Arguments();

        Logger.LogDebug("Dispatching {Command} command", command);
        if (arguments.Any()) Logger.LogDebug("Arguments: {Arguments}", string.Join(", ", arguments));

        return receivedCommandMessage;
    }

    Command? Match(string receivedCommandText) => _commands.FirstOrDefault(command => command.Matches(receivedCommandText));
}