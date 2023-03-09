using Telegram.CallbackQueries;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Videos;

static class VideoProcessingExtensions
{
    internal static IServiceCollection AddVideoProcessing(this IServiceCollection services)
        => services
        .AddCallbackQueries()
        .AddMediaFilesManager()
        .AddScoped<ProcessingMethodSelectorVideoHandler>()
        .AddScoped<ICallbackQueryHandler, VideoProcessingCallbackQueryHandler>()
        .AddTasks();
}
