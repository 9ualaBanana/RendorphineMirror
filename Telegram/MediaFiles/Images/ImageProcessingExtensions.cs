using Telegram.CallbackQueries;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImageProcessingExtensions
{
    internal static IServiceCollection AddImageProcessing(this IServiceCollection services)
        => services
        .AddCallbackQueries()
        .AddMediaFilesManager()
        .AddScoped<ProcessingMethodSelectorImageHandler>()
        .AddScoped<ICallbackQueryHandler, ImageProcessingCallbackQueryHandler>()
        .AddTasks();
}
