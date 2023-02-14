using Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static IServiceCollection AddUpdateRouting(this IServiceCollection services)
        => services
        .AddScoped<UpdateRoutingBranchingMiddleware>()
        .AddScoped<UpdateReaderMiddleware>()
        .AddUpdateTypeRouting();

    internal static IApplicationBuilder UseUpdateRouting(this IApplicationBuilder app)
        => app.UseMiddleware<UpdateRoutingBranchingMiddleware>();
}
