using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static IServiceCollection AddUpdateRouting(this IServiceCollection services)
        => services
        .AddScoped<UpdateRoutingBranchingMiddleware>()
        .AddScoped<UpdateReaderMiddleware>()
        .AddScoped<UpdateTypeRouterMiddleware>();

    internal static IApplicationBuilder UseUpdateRouting(this IApplicationBuilder app)
        => app.UseMiddleware<UpdateRoutingBranchingMiddleware>();
}
