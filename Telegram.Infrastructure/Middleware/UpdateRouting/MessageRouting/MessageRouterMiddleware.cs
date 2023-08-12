using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

public class MessageRouterMiddleware : IUpdateTypeRouter
{
    readonly IEnumerable<IMessageRouter> _messageRouters;

    readonly ILogger _logger;

    public MessageRouterMiddleware(IEnumerable<IMessageRouter> messageRouters, ILogger<MessageRouterMiddleware> logger)
    {
        _messageRouters = messageRouters;
        _logger = logger;
    }

    public bool Matches(Update update) => update.Type is UpdateType.Message;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_messageRouters.Switch(context.GetUpdate().Message!) is IMiddleware messageRouter)
        {
            _logger.LogTrace("Invoking {Router}", messageRouter.GetType().Name);
            await messageRouter.InvokeAsync(context, next);
        }
        else _logger.LogTrace("No appropriate message router was found");
    }
}
