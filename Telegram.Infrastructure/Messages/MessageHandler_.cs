﻿using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Telegram.Infrastructure.Messages;

public abstract class MessageHandler_ : UpdateHandler_
{
    /// <summary>
    /// Unique identifier for the chat where the <see cref="Telegram.Bot.Types.Message"/> being handled came from.
    /// </summary>
    protected ChatId ChatId => Message.Chat.Id;

    /// <summary>
    /// The <see cref="Bot.Types.Message"/> being handled.
    /// </summary>
    protected virtual Message Message => Update.Message!;

    protected MessageHandler_(
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
    {
    }
}