using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Messages;
using Telegram.Localization.Resources;

namespace Telegram.Security.Authentication;

public class AuthenticationMessageHandler : MessageHandler
{
    readonly AuthenticationManager _authenticationManager;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

    public AuthenticationMessageHandler(
        AuthenticationManager authenticationManager,
        LocalizedText.Authentication localizedAuthenticationText,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationMessageHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _authenticationManager = authenticationManager;
        _localizedAuthenticationText = localizedAuthenticationText;
    }

    public override bool Matches(Message message)
        => message.Text is string potentialCredentials
        && potentialCredentials.Split() is var splitPotentialCredentials
        && splitPotentialCredentials.Length is 2
        && splitPotentialCredentials.First() is var email && email.Contains('@');

    public override async Task HandleAsync()
    {
        var credentials = Message.Text!.Split();
        var (email, password) = (credentials.First(), credentials.Last());

        var user = await _authenticationManager.GetBotUserAsyncWith(ChatId);

        if (user.IsAuthenticatedByMPlus)
            await Bot.SendMessageAsync_(ChatId,
                await _localizedAuthenticationText.AlreadyLoggedInAsync(ChatId, user.MPlusIdentity!, RequestAborted),
                cancellationToken: RequestAborted);
        else await _authenticationManager.TryAuthenticateByMPlusAsync(user, email, password, RequestAborted);
    }
}
