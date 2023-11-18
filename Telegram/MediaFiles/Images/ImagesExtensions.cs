using GIBS;
using GIBS.CallbackQueries;
using GIBS.Media.Images;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImagesExtensions
{
    internal static ITelegramBotBuilder AddImages(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped_<ICallbackQueryHandler, ImageProcessingCallbackQueryHandler>();
        builder.Services.TryAddScoped_<ImageHandler_, ProcessingMethodSelectorImageHandler>();
        builder.AddRTasks();

        return builder;
    }
}
