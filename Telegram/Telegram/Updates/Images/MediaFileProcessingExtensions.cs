using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Services;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Images;

public static class MediaFileProcessingExtensions
{
    public static IServiceCollection AddTelegramImageProcessing(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<TelegramImageHandler>()
            .AddScoped<TelegramVideoHandler>()
            .AddScoped<ImageProcessingCallbackQueryHandler>()
            .AddScoped<VectorizerCallbackQueryHandler>()
            .AddScoped<VideoProcessingCallbackQueryHandler>()
            .AddSingleton<TaskRegistry>()
            .AddSingleton<TelegramFileRegistry>()
            .AddScoped<TaskCallbackQueryHandler>();
}
