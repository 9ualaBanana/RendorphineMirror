using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.CallbackQueries;

public class CallbackQueryRouterMiddleware : IUpdateTypeRouter
{
    public bool Matches(Update update) => update.Type is UpdateType.CallbackQuery;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    { context.Request.Path += $"/{CallbackQueriesController.PathFragment}"; await next(context); }
}
