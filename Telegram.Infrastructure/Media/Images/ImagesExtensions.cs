using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Messages;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Media.Images;

static class ImagesExtensions
{
    internal static ITelegramBotBuilder AddImagesCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped_<IMessageRouter, ImagesRouterMiddleware>();
        builder.AddMediaFiles();

        return builder;
    }
}
