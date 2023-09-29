using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles.Images;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

static class ImagesExtensions
{
    internal static ITelegramBotBuilder AddImages(this ITelegramBotBuilder builder)
    {
        builder.AddCallbackQueries();
        builder.Services.TryAddScoped_<ICallbackQueryHandler, ImageProcessingCallbackQueryHandler>();
        builder.Services.TryAddScoped<ProcessingMethodSelectorImageHandler>();
        builder
            .AddImagesCore()
            .AddRTasks();

        return builder;
    }
}
