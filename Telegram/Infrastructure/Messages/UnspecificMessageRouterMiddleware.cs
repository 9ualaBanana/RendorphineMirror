using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Messages;

public class UnspecificMessageRouterMiddleware : MessageRouter
{
    protected override string PathFragment => MessagesController.PathFragment;

    public override bool Matches(Message message) => true;
}
