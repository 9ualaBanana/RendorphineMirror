﻿using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Localization.Resources;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

public class StartCommand : CommandHandler
{
    readonly string _loginCommand;
    readonly AuthenticationManager _authenticationManager;
    readonly MPlusClient _mPlusClient;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

    public StartCommand(
        LoginCommand loginCommandHandler,
        AuthenticationManager authenticationManager,
        MPlusClient mPlusClient,
        LocalizedText.Authentication localizedAuthenticationMessage,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StartCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _loginCommand = loginCommandHandler.Target.Prefixed;
        _authenticationManager = authenticationManager;
        _mPlusClient = mPlusClient;
        _localizedAuthenticationText = localizedAuthenticationMessage;
    }

    internal override Command Target => CommandFactory.Create("start");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (!receivedCommand.UnquotedArguments.Any())
            await SendStartMessageAsync();
        else if (receivedCommand.UnquotedArguments.FirstOrDefault() is string sessionId)
            await AuthenticateByMPlusViaBrowserAsyncWith(sessionId);
        else
        {
            var exception = new ArgumentNullException(nameof(sessionId),
                $"Required {nameof(sessionId)} argument to {Target.Prefixed} is missing.");
            Logger.LogCritical("M+ authentication via browser failed.");
            throw exception;
        }


        async Task SendStartMessageAsync()
        {
            var user = await _authenticationManager.PersistTelegramUserAsyncWith(ChatId, save: false, RequestAborted);

            if (user.IsAuthenticatedByMPlus)
                await Bot.SendMessageAsync_(ChatId,
                    _localizedAuthenticationText.HowToUse,
                    cancellationToken: RequestAborted);
            else
            {
                await Bot.SendMessageAsync_(ChatId,
                    _localizedAuthenticationText.Start(_loginCommand),
                    InlineKeyboardButton.WithLoginUrl("Login", new() { BotUsername = "testMicrostockPlusBot", Url = "https://3f61-82-211-155-73.ngrok-free.app/authenticate/result", RequestWriteAccess = true }),
                    //InlineKeyboardButton.WithUrl(
                    //    _localizedAuthenticationText.BrowserAuthenticationButton,
                    //    "https://microstock.plus/oauth2/authorize?clientid=003&state=_"),
                    disableWebPagePreview: true, cancellationToken: RequestAborted);
            }
        }

        async Task AuthenticateByMPlusViaBrowserAsyncWith(string sessionId)
        {
            var user = await _authenticationManager.PersistTelegramUserAsyncWith(ChatId, save: false, RequestAborted);

            if (!user.IsAuthenticatedByMPlus)
                await AuthenticateByMPlusAsyncWith(sessionId);
            else await _authenticationManager.SendAlreadyLoggedInMessageAsync(ChatId, RequestAborted);


            async Task AuthenticateByMPlusAsyncWith(string sessionId)
            {
                var publicSessionInfo = await _mPlusClient.TaskManager.GetPublicSessionInfoAsync(sessionId, RequestAborted);
                await _authenticationManager.PersistMPlusUserIdentityAsync(user, new(publicSessionInfo.ToMPlusIdentity()), RequestAborted);
                await _authenticationManager.SendSuccessfulLogInMessageAsync(ChatId, sessionId, RequestAborted);
            }
        }
    }
}
