using Telegram.Bot.Types;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Security.Authentication;

public class MessageAuthenticationMiddleware : IMessageRouter
{
    readonly AuthenticationManager _authenticationManager;

    public MessageAuthenticationMiddleware(AuthenticationManager authenticationManager)
    {
        _authenticationManager = authenticationManager;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var message = context.GetUpdate().Message!;
        var credentials = message.Text!.Split();
        var (email, password) = (credentials.First(), credentials.Last());

        var user = await _authenticationManager.PersistTelegramUserAsyncWith(message.Chat.Id, save: false, context.RequestAborted);

        if (user.IsAuthenticatedByMPlus)
            await _authenticationManager.SendAlreadyLoggedInMessageAsync(message.Chat.Id, context.RequestAborted);
        else await _authenticationManager.TryAuthenticateByMPlusAsync(user, email, password, context.RequestAborted);
    }

    public bool Matches(Message message)
        => message.Text is string potentialCredentials
        && potentialCredentials.Split() is var splitPotentialCredentials
        && splitPotentialCredentials.Length is 2
        && splitPotentialCredentials.First() is var email && email.Contains('@');
}

static class MessageAuthenticationMiddlewareExtensions
{
    internal static ITelegramBotBuilder AddMessageAuthentication(this ITelegramBotBuilder builder)
        => builder
            .AddMessageRouter<MessageAuthenticationMiddleware>()
            .AddAuthenticationManager();
}
