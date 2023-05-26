using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.MediaFiles.Videos;

static class VideosHelper
{
    internal static ITelegramBotBuilder AddVideosCore(this ITelegramBotBuilder builder)
        => builder
            .AddMessageRouter<VideosRouterMiddleware>()
            .AddMediaFilesCore();

    internal static bool IsVideo(this Message message)
        => message.Document.IsVideo() || message.Video is not null;

    internal static bool IsVideo(this Document? document)
        => document is not null && document.MimeType is not null && document.MimeType.StartsWith("video");
}
