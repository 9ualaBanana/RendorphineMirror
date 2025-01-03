﻿using Telegram.Bot.Types;
using Telegram.Services.Node;
using Telegram.Telegram;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;

namespace Telegram.Services.Telegram.Updates.Commands.Online;

public class AdminOnlineCommand : AdminAuthenticatedCommand
{
    readonly UserNodes _userNodes;


    public AdminOnlineCommand(
        ILogger<AuthenticatedCommand> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        UserNodes userNodes) : base(logger, bot, authenticator)
    {
        _userNodes = userNodes;
    }


    public override string Value => "adminonline";

    protected override async Task HandleAsyncCore(Update update, ChatAuthenticationToken authenticationToken)
    {
        var nodesOnline = _userNodes.Aggregate(0, (nodesOnline, theUserNodes) => nodesOnline += theUserNodes.Value.NodesOnline.Count);
        var nodesOffline = _userNodes.Aggregate(0, (nodesOffline, theUserNodes) => nodesOffline += theUserNodes.Value.NodesOffline.Count);

        var message = Logic.BuildMessage(nodesOnline, nodesOffline);

        await Bot.TrySendMessageAsync(update.Message!.Chat.Id, message);
    }
}
