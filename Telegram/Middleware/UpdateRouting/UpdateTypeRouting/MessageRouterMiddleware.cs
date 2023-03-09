using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Commands;
using Telegram.MediaFiles.Images;
using Telegram.MediaFiles.Videos;
using Telegram.Models;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class MessageRouterMiddleware : IUpdateTypeRouter
{
    public bool Matches(Update update) => update.Type is UpdateType.Message;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var message = context.GetUpdate().Message!;

        if (message.IsCommand())
            context.Request.Path += '/'+CommandsController.PathFragment;
        else if (message.IsVideo()) // Check for video must precede the one for image because Photo is not null for videos too.
            context.Request.Path += '/'+VideosController.PathFragment;
        else if (message.IsImage())
            context.Request.Path += '/'+ImagesController.PathFragment;
        else return;

        await next(context);
    }
}

static class MessageRouterMiddlewareHelpers
{
    internal static bool IsCommand(this Message message)
        => message.Text is not null && message.Text.StartsWith(Command.Prefix) && message.Text.Length > 1;

    internal static bool IsImage(this Message message)
        => message.Document.IsImage() || message.Photo is not null || Uri.IsWellFormedUriString(message.Text, UriKind.Absolute);
    internal static bool IsImage(this Document? document)
        => document is not null && document.MimeType is not null && document.MimeType.StartsWith("image");

    internal static bool IsVideo(this Message message) => message.Document.IsVideo() || message.Video is not null;
    static bool IsVideo(this Document? document)
        => document is not null && document.MimeType is not null && document.MimeType.StartsWith("video");
}
