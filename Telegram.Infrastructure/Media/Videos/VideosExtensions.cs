using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Media.Videos;

static class VideosExtensions
{
    internal static ITelegramBotBuilder AddVideosCore(this ITelegramBotBuilder builder)
    {
        builder.Services.AddScoped<IMessageRouter, VideosRouterMiddleware>();
        builder.AddMediaFiles();

        return builder;
    }
}
