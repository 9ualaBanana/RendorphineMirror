using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

public class CommandRouterMiddleware : MessageRouter
{
    protected override string PathFragment => CommandsController.PathFragment;

    public override bool Matches(Message message)
        => message.Text is not null && message.Text.StartsWith(Command.Prefix) && message.Text.Length > 1;
}
