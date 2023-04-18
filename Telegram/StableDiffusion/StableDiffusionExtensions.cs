using Telegram.Infrastructure.CallbackQueries;

namespace Telegram.StableDiffusion;

static class StableDiffusionExtensions
{
    internal static IServiceCollection AddStableDiffusion(this IServiceCollection services)
        => services
        .AddSingleton<StableDiffusionPrompt>().AddSingleton<StableDiffusionPrompt.CachedMessages>()
        .AddScoped<GeneratedStableDiffusionImages>()
        .AddScoped<ICallbackQueryHandler, StableDiffusionCallbackQueryHandler>();
}
