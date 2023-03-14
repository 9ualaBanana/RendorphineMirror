using Telegram.Bot.Types;

namespace Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

public interface IMessageRouter : ISwitchableMiddleware<IMessageRouter, Message>
{
}
