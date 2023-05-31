using Telegram.Infrastructure;
using Telegram.Infrastructure.CallbackQueries;

namespace Telegram.StableDiffusion;

static class StableDiffusionExtensions
{
    internal static IServiceCollection AddStableDiffusion(this IServiceCollection services)
    {
        services.TryAddScoped_<ICallbackQueryHandler, StableDiffusionCallbackQueryHandler>();
        return services
            .AddSingleton<StableDiffusionPrompt>().AddSingleton<StableDiffusionPrompt.CachedMessages>()
            .AddScoped<GeneratedStableDiffusionImages>();

    }
}
