using Telegram.Services.Tasks;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Services.Telegram.Updates.Tasks;

namespace Telegram.Services.Telegram.Updates.Images;

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
