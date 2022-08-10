using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Commands;

public class TelegramCommandHandler
{
    readonly ILogger _logger;

    readonly IEnumerable<Command> _commands;

    public TelegramCommandHandler(ILogger<TelegramMessageHandler> logger, IEnumerable<Command> commands)
    {
        _logger = logger;
        _commands = commands;
    }

    public async Task HandleAsync(Update update)
    {
        var receivedCommand = GetCommandFrom(update);
        var command = Match(receivedCommand);
        if (command is not null) await command.HandleAsync(update);
        else _logger.LogDebug("No handler for {Command} command is found", receivedCommand);
    }

    string GetCommandFrom(Update update)
    {
        var receivedCommandMessage = update.Message!.Text!;
        var command = receivedCommandMessage.Command();
        var arguments = receivedCommandMessage.Arguments();

        _logger.LogDebug("Dispatching {Command} command", command);
        if (arguments.Any()) _logger.LogDebug("Arguments: {Arguments}", string.Join(", ", arguments));

        return receivedCommandMessage;
    }

    Command? Match(string receivedCommand) => _commands.FirstOrDefault(command => command.Matches(receivedCommand));
}