using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.MediaFiles.Videos;

namespace Telegram.Infrastructure.MediaFiles.Videos;

public class VideoRouterMiddleware : IMessageRouter
{
    public bool Matches(Message message) => message.IsVideo();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    { context.Request.Path += $"/{VideosController.PathFragment}"; await next(context); }
}
