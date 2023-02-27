using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Middleware.UpdateRouting;

/// <summary>
/// Redirects requests from Telegram containing <see cref="Update"/>s to corresponding update routing branch.
/// </summary>
public class UpdateRoutingBranchingMiddleware : IMiddleware
{
    readonly TelegramBotOptions _botOptions;
    readonly UpdateReaderMiddleware _updateContextConstructorMiddleware;

    public UpdateRoutingBranchingMiddleware(
        IOptions<TelegramBotOptions> botOptions,
        UpdateReaderMiddleware updateContextConstructorMiddleware)
    {
        _botOptions = botOptions.Value;
        _updateContextConstructorMiddleware = updateContextConstructorMiddleware;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) => await
        (context.Request.Path.HasValue && context.Request.Path.Value.Contains(_botOptions.Token) ?
        _updateContextConstructorMiddleware.InvokeAsync(context, next) : next(context));
}
