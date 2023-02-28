using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Commands;
using Telegram.Controllers;
using Telegram.Models;
using Telegram.Telegram.Updates;
using Command = Telegram.Commands.Command;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

public class MessageRouterMiddleware : IUpdateTypeRouter
{
    readonly TelegramBot _bot;

    readonly ILogger _logger;

    public MessageRouterMiddleware(TelegramBot bot, ILogger<MessageRouterMiddleware> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public bool Matches(Update update) => update.Type is UpdateType.Message;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var message = context.GetUpdate().Message!;

        if (message.IsCommand())
            context.Request.Path += '/'+CommandsController.PathFragment;
        else if (message.IsVideo()) // Check for video must precede the one for image because Photo is not null for videos too.
            context.Request.Path += '/'+VideoController.PathFragment;
        else if (message.IsImage())
            context.Request.Path += '/'+ImageController.PathFragment;
        else
        {
            if (message.IsSystemMessageOf(_bot))
                // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
                _logger.LogTrace("System messages are handled by {Handler}", nameof(TelegramChatMemberUpdatedHandler));
            return;
        }

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

    internal static bool IsSystemMessageOf(this Message message, TelegramBotClient bot)
        => message.LeftChatMember?.Id == bot.BotId || message.NewChatMembers?.First().Id == bot.BotId;
}
