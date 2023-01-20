using Telegram.Middleware.UpdateRouting.UpdateTypeRouting;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static IServiceCollection AddUpdateRouting(this IServiceCollection services)
        => services
        .AddScoped<UpdateContextCache>()
        .AddScoped<UpdateContextConstructorMiddleware>()
        .AddUpdateTypeRouting();

    internal static IApplicationBuilder UseUpdateRouting(this IApplicationBuilder app)
        => app
        //.UseWhen(context => context.Request.Host == new HostString("api.telegram.org"), app =>
        //{
        //    Move statements from below here when 
        //});
        .UseMiddleware<UpdateContextConstructorMiddleware>()
        .UseUpdateTypeRouting();
}
