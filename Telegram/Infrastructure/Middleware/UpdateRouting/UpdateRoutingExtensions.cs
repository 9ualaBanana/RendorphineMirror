using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static ITelegramBotBuilder AddUpdateRouting(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<UpdateRoutingMiddleware>();
        builder.Services.TryAddScoped<UpdateReaderMiddleware>();
        builder.Services.TryAddScoped<UpdateTypeRouterMiddleware>();

        return builder;
    }
        

    internal static WebApplication UseUpdateRouting(this WebApplication app)
    {
        app.MapControllers();
        app.UseMiddleware<UpdateRoutingMiddleware>().UseRouting();
        return app;
    }
        
}
