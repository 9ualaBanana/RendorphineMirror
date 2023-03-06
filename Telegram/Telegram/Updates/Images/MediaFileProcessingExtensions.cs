using Telegram.Models;
using Telegram.Tasks;
using Telegram.Tasks.CallbackQuery;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Services;

namespace Telegram.Telegram.Updates.Images;

public static class MediaFileProcessingExtensions
{
    public static IServiceCollection AddTelegramImageProcessing(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<MediaFileDownloader>()
            .AddScoped<TelegramImageHandler>()
            .AddScoped<TelegramVideoHandler>()
            .AddScoped<ImageProcessingCallbackQueryHandler>()
            .AddScoped<VectorizerCallbackQueryHandler>()
            .AddScoped<VideoProcessingCallbackQueryHandler>()
            .AddSingleton<RegisteredTasksCache>()
            .AddSingleton<CachedMediaFiles>()
            .AddScoped<TaskCallbackQueryHandler>();
}
