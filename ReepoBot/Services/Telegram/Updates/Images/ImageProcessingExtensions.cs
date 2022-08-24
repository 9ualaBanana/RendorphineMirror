using ReepoBot.Services.Tasks;
using ReepoBot.Services.Telegram.FileRegistry;
using ReepoBot.Services.Telegram.Updates.Tasks;

namespace ReepoBot.Services.Telegram.Updates.Images;

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
