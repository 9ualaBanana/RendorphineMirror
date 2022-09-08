﻿using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Commands;

public abstract class AdminAuthenticatedCommand : AuthenticatedCommand
{
    public AdminAuthenticatedCommand(
    ILogger<AuthenticatedCommand> logger,
    TelegramBot bot,
    ChatAuthenticator authenticator) : base(logger, bot, authenticator)
    {
    }


    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        if (authenticationToken.MPlus.IsAdmin)
            await HandleAsyncCore(update, authenticationToken);
    }

    protected abstract Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken);
}
