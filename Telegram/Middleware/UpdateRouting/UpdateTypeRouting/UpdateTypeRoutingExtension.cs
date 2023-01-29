namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

internal static class UpdateTypeRoutingExtensions
{
    internal static IServiceCollection AddUpdateTypeRouting(this IServiceCollection services) => services
        .AddScoped<UpdateTypeRouterMiddleware>()
        .AddScoped<IUpdateTypeRouter, MessageRouterMiddleware>()
        .AddScoped<IUpdateTypeRouter, CallbackQueryRouterMiddleware>()
        .AddScoped<IUpdateTypeRouter, MyChatMemberRouterMiddleware>();
}
