﻿using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Localization.Resources;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;
using Telegram.Security.Authentication;
using Telegram.TrialUsers;

namespace Telegram.Commands.Handlers;

public class StartCommand : CommandHandler
{
    readonly string _loginCommand;
    readonly AuthenticationManager _authenticationManager;
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot.Options _botOptions;
    readonly LinkGenerator _linkGenerator;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

    public StartCommand(
        LoginCommand loginCommandHandler,
        AuthenticationManager authenticationManager,
        MPlusClient mPlusClient,
        IOptions<TelegramBot.Options> botOptions,
        LinkGenerator linkGenerator,
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
        _botOptions = botOptions.Value;
        _linkGenerator = linkGenerator;
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
            var user = await _authenticationManager.GetBotUserAsyncWith(ChatId);

            if (user.IsAuthenticatedByMPlus)
                await Bot.SendMessageAsync_(ChatId,
                    _localizedAuthenticationText.HowToUse,
                    cancellationToken: RequestAborted);
            else
            {
                await Bot.SendMessageAsync_(ChatId,
                    _localizedAuthenticationText.Start(_loginCommand),
                    new InlineKeyboardMarkup(new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl(
                            _localizedAuthenticationText.BrowserAuthenticationButton,
                            "https://microstock.plus/oauth2/authorize?clientid=003&state=_"),

                        InlineKeyboardButton.WithLoginUrl(
                            _localizedAuthenticationText.LoginAsGuestButton, new()
                            {
                                BotUsername = _botOptions.Username,
                                Url = GuestLoginUrlForUserWith(ChatId),
                                RequestWriteAccess = true
                            })
                    }),
                    disableWebPagePreview: true, cancellationToken: RequestAborted);


                string GuestLoginUrlForUserWith(ChatId chatId)
                {
                    // Kludge that excludes `TelegramBot.Options.PathBase` from the link generated by `LinkGenerator`.
                    Context.Request.PathBase = PathString.Empty;

                    return QueryHelpers.AddQueryString(
                        _linkGenerator.GetUriByName(Context, TrialUsersAuthenticationController.using_telegram_login_widget_data, host: HostString.FromUriComponent(_botOptions.Host.Authority))!,
                        "chatId", chatId.ToString());
                    // Resulting Telegram Login Widget data returned as query string parameters will be appended to the callback's query string if present.
                }
            }
        }

        async Task AuthenticateByMPlusViaBrowserAsyncWith(string sessionId)
        {
            var user = await _authenticationManager.GetBotUserAsyncWith(ChatId);

            if (!user.IsAuthenticatedByMPlus)
                await AuthenticateByMPlusAsyncWith(sessionId);
            else await _authenticationManager.SendAlreadyLoggedInMessageAsync(ChatId, user.MPlusIdentity, RequestAborted);


            async Task AuthenticateByMPlusAsyncWith(string sessionId)
            {
                var publicSessionInfo = await _mPlusClient.TaskManager.GetPublicSessionInfoAsync(sessionId, RequestAborted);
                await _authenticationManager.AddMPlusIdentityAsync(user, new(publicSessionInfo.ToMPlusIdentity()), RequestAborted);
                await _authenticationManager.SendSuccessfulLogInMessageAsync(ChatId, user.MPlusIdentity!, RequestAborted);
            }
        }
    }
}
