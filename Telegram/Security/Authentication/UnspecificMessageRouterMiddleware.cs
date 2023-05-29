using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Security.Authentication;

public class UnspecificMessageRouterMiddleware : MessageRouter
{
    protected override string PathFragment => MessageController.PathFragment;

    public override bool Matches(Message message) => true;
}
