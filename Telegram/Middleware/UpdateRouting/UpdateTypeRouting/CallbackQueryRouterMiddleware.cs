using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class CallbackQueryRouterMiddleware : IUpdateTypeRouter
{
    readonly ILogger _logger;

    public CallbackQueryRouterMiddleware(ILogger<CallbackQueryRouterMiddleware> logger)
    {
        _logger = logger;
    }

    public bool Matches(Update update) => update.Type is UpdateType.CallbackQuery;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);
    }
}
