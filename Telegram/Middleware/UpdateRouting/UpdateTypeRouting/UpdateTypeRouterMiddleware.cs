﻿using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class UpdateTypeRouterMiddleware : IMiddleware
{
    readonly ILogger _logger;

    readonly IEnumerable<IUpdateTypeRouter> _updateTypeRouters;

    public UpdateTypeRouterMiddleware(
        IEnumerable<IUpdateTypeRouter> updateTypeRouters,
        ILogger<UpdateTypeRouterMiddleware> logger)
    {
        _updateTypeRouters = updateTypeRouters;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogTrace("{UpdateType} update is received", context.GetUpdate().Type);

        if (_updateTypeRouters.Switch(context.GetUpdate()) is IMiddleware updateTypeRouter)
        {
            _logger.LogTrace("Invoking {Router}", updateTypeRouter.GetType().Name);
            await updateTypeRouter.InvokeAsync(context, next);
        }
        else _logger.LogWarning("No appropriate router for {UpdateType} update was found", context.GetUpdate().Type);
    }
}
