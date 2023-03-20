using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.MediaFiles;
using Telegram.Tasks;

namespace Telegram.Infrastructure.MediaFiles.Videos;

static class VideosExtensions
{
    internal static IServiceCollection AddVideosCore(this IServiceCollection services)
        => services
        .AddScoped<MessageRouter, VideoRouterMiddleware>()
        .AddMediaFiles()
        .AddTasks();

    internal static bool IsVideo(this Message message)
        => message.Document.IsVideo() || message.Video is not null;

    internal static bool IsVideo(this Document? document)
        => document is not null && document.MimeType is not null && document.MimeType.StartsWith("video");
}
