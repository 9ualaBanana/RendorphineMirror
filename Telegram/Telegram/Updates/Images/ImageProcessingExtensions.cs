using Telegram.Services.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Services;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Images;

public static class ImageProcessingExtensions
{
    public static IServiceCollection AddTelegramImageProcessing(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<TelegramImageHandler>()
            .AddScoped<ImageProcessingCallbackQueryHandler>()
            .AddSingleton<TaskRegistry>()
            .AddSingleton<TelegramFileRegistry>()
            .AddScoped<TaskCallbackQueryHandler>();
}
