﻿using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public abstract class AuthenticatedCommand : Command
{
    protected readonly TelegramChatIdAuthenticator Authenticator;



    public AuthenticatedCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        TelegramChatIdAuthenticator authenticator) : base(logger, bot)
    {
        Authenticator = authenticator;
    }



    public override async Task HandleAsync(Update update)
    {
        var chatId = update.Message!.Chat.Id;
        var authenticationToken = Authenticator.TryGetTokenFor(chatId);

        if (authenticationToken is not null) await HandleAsync(update, authenticationToken);
    }

    protected abstract Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken);
}
