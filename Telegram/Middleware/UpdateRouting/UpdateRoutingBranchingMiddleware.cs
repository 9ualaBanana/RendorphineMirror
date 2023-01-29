﻿using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace Telegram.Middleware.UpdateRouting;

public class UpdateRoutingBranchingMiddleware : IMiddleware
{
    readonly TelegramBotOptions _botOptions;
    readonly UpdateContextConstructorMiddleware _updateContextConstructorMiddleware;

    public UpdateRoutingBranchingMiddleware(
        IOptions<TelegramBotOptions> botOptions,
        UpdateContextConstructorMiddleware updateContextConstructorMiddleware)
    {
        _botOptions = botOptions.Value;
        _updateContextConstructorMiddleware = updateContextConstructorMiddleware;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) => await
        (context.Request.Path.HasValue && context.Request.Path.Value.Contains(_botOptions.Token) ?
        _updateContextConstructorMiddleware.InvokeAsync(context, next) : next(context));
}
