using Telegram.Bot.Types;

namespace Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

public interface IMessageRouter : ISwitchableMiddleware<IMessageRouter, Message>
{
}

public abstract class MessageRouter : IMessageRouter
{
    protected abstract string PathFragment { get; }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Request.Path += $"/{PathFragment}";
        await next(context);
    }

    public abstract bool Matches(Message message);
}
