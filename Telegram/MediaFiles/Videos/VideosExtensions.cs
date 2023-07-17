using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Videos;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Videos;

static class VideosExtensions
{
    internal static ITelegramBotBuilder AddVideos(this ITelegramBotBuilder builder)
    {
        builder.AddCallbackQueries();
        builder.Services.TryAddScoped_<ICallbackQueryHandler, VideoProcessingCallbackQueryHandler>();
        builder.Services.TryAddScoped<ProcessingMethodSelectorVideoHandler>();
        builder
            .AddVideosCore()
            .AddTasks();

        return builder;
    }
}
