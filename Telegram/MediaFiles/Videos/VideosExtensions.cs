using GIBS;
using GIBS.CallbackQueries;
using GIBS.Media.Videos;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Videos;

static class VideosExtensions
{
    internal static ITelegramBotBuilder AddVideos(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped_<ICallbackQueryHandler, VideoProcessingCallbackQueryHandler>();
        builder.Services.TryAddScoped<VideoHandler_, ProcessingMethodSelectorVideoHandler>();
        builder.AddTasks();

        return builder;
    }
}
