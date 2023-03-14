using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

public class CommandRouterMiddleware : IMessageRouter
{
    public bool Matches(Message message)
        => message.Text is not null && message.Text.StartsWith(Command.Prefix) && message.Text.Length > 1;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    { context.Request.Path += $"/{CommandsController.PathFragment}"; await next(context); }
}
