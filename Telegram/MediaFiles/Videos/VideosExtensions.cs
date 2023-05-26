using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Videos;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Videos;

static class VideosExtensions
{
    internal static ITelegramBotBuilder AddVideos(this ITelegramBotBuilder builder)
    {
        builder
            .AddCallbackQueryHandler<VideoProcessingCallbackQueryHandler>()
            .AddCallbackQueries()
            .AddVideosCore()
            .AddTasks()

            .Services
            .AddScoped<ProcessingMethodSelectorVideoHandler>();
        return builder;
    }
}
