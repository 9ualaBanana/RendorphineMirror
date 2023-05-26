using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.Middleware.UpdateRouting;

internal static class UpdateRoutingExtensions
{
    internal static ITelegramBotBuilder AddUpdateRouting(this ITelegramBotBuilder builder)
    {
        builder.Services
            .AddScoped<UpdateRoutingBranchingMiddleware>()
            .AddScoped<UpdateReaderMiddleware>()
            .AddScoped<UpdateTypeRouterMiddleware>();
        return builder;
    }
        

    internal static WebApplication UseUpdateRouting(this WebApplication app)
    {
        app.MapControllers();
        app.UseMiddleware<UpdateRoutingBranchingMiddleware>().UseRouting();
        return app;
    }
        
}
