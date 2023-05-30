using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Middleware.UpdateRouting;

/// <summary>
/// Redirects requests from Telegram containing <see cref="Update"/>s to corresponding update routing branch.
/// </summary>
/// <remarks>
/// Abstracts routing by rewriting the request path to remove <see cref="TelegramBot.Options.Token"/>
/// and move the rest to <see cref="HttpRequest.PathBase"/> which is not considered in routing.
/// </remarks>
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

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.HasValue && context.Request.Path.Value.Contains(_botOptions.Token))
        {
            RewriteRequestPath();
            await _updateReaderMiddleware.InvokeAsync(context, next);
        }
        else await next(context);


        void RewriteRequestPath()
        {
            context.Request.PathBase = PathString.FromUriComponent(
                context.Request.Path.Value.Replace(_botOptions.Token, string.Empty).TrimEnd('/'));
            context.Request.Path = PathString.Empty;
        }
    }
}
