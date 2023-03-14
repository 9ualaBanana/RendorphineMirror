using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Videos;

namespace Telegram.MediaFiles.Videos;

static class VideosExtensions
{
    internal static IServiceCollection AddVideos(this IServiceCollection services)
        => services
        .AddScoped<ProcessingMethodSelectorVideoHandler>()
        .AddScoped<ICallbackQueryHandler, VideoProcessingCallbackQueryHandler>()
        .AddVideosCore();
}
