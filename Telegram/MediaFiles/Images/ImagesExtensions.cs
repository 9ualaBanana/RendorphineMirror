using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Media.Images;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImagesExtensions
{
    internal static ITelegramBotBuilder AddImages(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped_<ICallbackQueryHandler, ImageProcessingCallbackQueryHandler>();
        builder.Services.TryAddScoped_<ImageHandler_, ProcessingMethodSelectorImageHandler>();
        builder.AddTasks();

        return builder;
    }
}
