using ReepoBot.Services.Telegram.FileRegistry;

namespace ReepoBot.Services.Telegram.Updates.Images;

public static class ImageProcessingExtensions
{
    public static IServiceCollection AddTelegramImageProcessing(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<TelegramImageHandler>()
            .AddScoped<ImageProcessingCallbackQueryHandler>()
            .AddSingleton<TelegramFileRegistry>()
            .AddScoped<TaskCallbackQueryHandler>();
}
