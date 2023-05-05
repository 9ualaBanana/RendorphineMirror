using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

public class CommandRouterMiddleware : MessageRouter
{
    protected override string PathFragment => CommandsController.PathFragment;

    readonly Command.Received _receivedCommand;
    readonly Command.Parser _commandParser;

    public CommandRouterMiddleware(Command.Received receivedCommand, Command.Parser commandParser)
    {
        _receivedCommand = receivedCommand;
        _commandParser = commandParser;
    }

    public override bool Matches(Message message)
    {
        if (message.Text is string potentialCommand && _commandParser.TryParse(potentialCommand) is Command command)
        { _receivedCommand.Set(command); return true; }
        else return false;
    }
}
