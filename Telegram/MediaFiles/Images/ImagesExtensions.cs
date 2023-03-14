using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Images;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImagesExtensions
{
    internal static IServiceCollection AddImages(this IServiceCollection services)
        => services
        .AddScoped<ProcessingMethodSelectorImageHandler>()
        .AddScoped<ICallbackQueryHandler, ImageProcessingCallbackQueryHandler>()
        .AddImagesCore()
        .AddTasks();
}
