using Microsoft.AspNetCore.Localization;
using Telegram.Bot;
using Telegram.Infrastructure;
using Telegram.Localization.Resources;

namespace Telegram.Localization;

static class LocalizationExtensions
{
    internal static IServiceCollection AddLocalization_(this IServiceCollection services)
    {
        var supportedCultures = new string[] { "en", "ru" };
        return services
            .AddLocalization()
            .AddRequestLocalization(_ => _
                .SetDefaultCulture(supportedCultures.First())
                .AddSupportedUICultures(supportedCultures)
                .AddInitialRequestCultureProvider(new CustomRequestCultureProvider(UpdateCultureProvider))
                )
            .AddLocalizedTexts();


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async static Task<ProviderCultureResult?> UpdateCultureProvider(HttpContext context)
            => context.ContainsUpdate() ?
            new ProviderCultureResult(context.GetUpdate().From().LanguageCode) : null;
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }

    static IServiceCollection AddLocalizedTexts(this IServiceCollection services)
        => services
        .AddSingleton<LocalizedText.Authentication>()
        .AddSingleton<LocalizedText.Media>();
}
