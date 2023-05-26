using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Images;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImagesExtensions
{
    internal static ITelegramBotBuilder AddImages(this ITelegramBotBuilder builder)
    {
        builder
            .AddCallbackQueryHandler<ImageProcessingCallbackQueryHandler>()
            .AddCallbackQueries()
            .AddImagesCore()
            .AddTasks()

            .Services
            .AddScoped<ProcessingMethodSelectorImageHandler>();
        return builder;
    }
}
