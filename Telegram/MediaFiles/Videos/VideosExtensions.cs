using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Media.Videos;
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
