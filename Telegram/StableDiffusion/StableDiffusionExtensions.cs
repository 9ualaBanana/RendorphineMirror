using GIBS;
using GIBS.CallbackQueries;

namespace Telegram.StableDiffusion;

static class StableDiffusionExtensions
{
    internal static IServiceCollection AddStableDiffusion(this IServiceCollection services)
    {
        services.TryAddScoped_<ICallbackQueryHandler, StableDiffusionCallbackQueryHandler>();
        return services
            .AddScoped<StableDiffusionPrompt>().AddSingleton<StableDiffusionPrompt.CachedMessages>()
            .AddScoped<GeneratedStableDiffusionImages>();

    }
}
