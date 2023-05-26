using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Middleware.UpdateRouting;

/// <summary>
/// Redirects requests from Telegram containing <see cref="Update"/>s to corresponding update routing branch.
/// </summary>
public class UpdateRoutingBranchingMiddleware : IMiddleware
{
    readonly TelegramBot.Options _botOptions;
    readonly UpdateReaderMiddleware _updateReaderMiddleware;

    public UpdateRoutingBranchingMiddleware(
        IOptions<TelegramBot.Options> botOptions,
        UpdateReaderMiddleware updateReaderMiddleware)
    {
        _botOptions = botOptions.Value;
        _updateReaderMiddleware = updateReaderMiddleware;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) => await
        (context.Request.Path.HasValue && context.Request.Path.Value.Contains(_botOptions.Token) ?
        _updateReaderMiddleware.InvokeAsync(context, next) : next(context));
}
