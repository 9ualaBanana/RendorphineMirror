using Telegram.Middleware.UpdateRouting.UpdateTypeRouting;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static IServiceCollection AddUpdateRouting(this IServiceCollection services)
        => services
        .AddScoped<UpdateRoutingBranchingMiddleware>()
        .AddScoped<UpdateContextCache>()
        .AddScoped<UpdateContextConstructorMiddleware>()
        .AddUpdateTypeRouting();

    internal static IApplicationBuilder UseUpdateRouting(this IApplicationBuilder app)
        => app.UseMiddleware<UpdateRoutingBranchingMiddleware>();
}
