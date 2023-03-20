using Telegram.Bot.Types;

namespace Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

public abstract class MessageRouter : ISwitchableMiddleware<MessageRouter, Message>
{
    protected abstract string PathFragment { get; }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Request.Path += $"/{PathFragment}";
        await next(context);
    }

    public abstract bool Matches(Message message);
}
