using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Persistence;
using Telegram.Localization.Resources;
using Telegram.MPlus;
using Telegram.MPlus.Clients;

namespace Telegram.Security.Authentication;

public class AuthenticationManager
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot _bot;
	readonly TelegramBotDbContext _database;
    readonly LocalizedText.Authentication _localiedAuthenticationMessage;

	public AuthenticationManager(
        MPlusClient mPlusClient,
        TelegramBot bot,
        TelegramBotDbContext database,
        LocalizedText.Authentication localizedAuthenticationMessage)
	{
        _mPlusClient = mPlusClient;
        _bot = bot;
		_database = database;
        _localiedAuthenticationMessage = localizedAuthenticationMessage;
	}

    internal async Task TryAuthenticateByMPlusAsync(TelegramBot.User.Entity user, string email, string password, CancellationToken cancellationToken)
    {
        if (await TryAuthenticateByMPlusAsync() is MPlusIdentityEntity identity)
        {
            await AddMPlusIdentityAsync(user, identity, cancellationToken);
            await SendSuccessfulLogInMessageAsync(user.ChatId, identity.SessionId, cancellationToken);
        }


        async Task<MPlusIdentityEntity?> TryAuthenticateByMPlusAsync()
        {
            try { return new MPlusIdentityEntity(await _mPlusClient.TaskManager.AuthenticateAsyncUsing(email, password)); }
            catch (Exception ex)
            {
                await _bot.SendMessageAsync_(user.ChatId,
                    $"{_localiedAuthenticationMessage.Failure}:\n" +
                ex.Message,
                    cancellationToken: cancellationToken);
                return null;
            }
        }
    }

    /// <summary>
    /// Persists <see cref="TelegramBot.User.Entity"/> with provided <paramref name="chatId"/> if it is not persisted yet
    /// or simplly returns the persisted <see cref="TelegramBot.User.Entity"/> from the database.
    /// </summary>
    /// <returns>
    /// Persisted <see cref="TelegramBot.User.Entity"/> who might be not logged in
    /// (i.e. its <see cref="TelegramBot.User.Entity.MPlusIdentity"/> might be <see langword="null"/>.
    /// </returns>
    internal async ValueTask<TelegramBot.User.Entity> GetBotUserAsyncWith(ChatId chatId)
    {
        var botUser = await _database.Users.FindAsync(chatId);
        if (botUser is null)
        {
            botUser = (await _database.Users.AddAsync(new(chatId))).Entity;
            await _database.SaveChangesAsync();
        }

        return botUser;
    }

    internal async Task AddMPlusIdentityAsync(
        TelegramBot.User.Entity user,
        MPlusIdentityEntity identity,
        CancellationToken cancellationToken)
    {
        user.MPlusIdentity = identity;
        _database.Update(user);

        await _database.SaveChangesAsync(cancellationToken);
    }

    internal async Task SendSuccessfulLogInMessageAsync(ChatId chatId, string sessionId, CancellationToken cancellationToken)
    {
        var balance = await _mPlusClient.TaskLauncher.RequestBalanceAsync(sessionId, cancellationToken);
        await _bot.SendMessageAsync_(chatId,
            $"{_localiedAuthenticationMessage.LoggedIn}\n\n" +
            $"{_localiedAuthenticationMessage.Balance(balance.RealBalance)}\n\n" +
            $"{_localiedAuthenticationMessage.HowToUse}",
            cancellationToken: cancellationToken);
    }

    internal async Task SendAlreadyLoggedInMessageAsync(ChatId chatId, CancellationToken cancellationToken)
        => await _bot.SendMessageAsync_(chatId,
            _localiedAuthenticationMessage.AlreadyLoggedIn,
            cancellationToken: cancellationToken);

    internal async Task SendSuccessfullLogOutMessageAsync(ChatId chatId, CancellationToken cancellationToken)
        => await _bot.SendMessageAsync_(chatId,
            _localiedAuthenticationMessage.LoggedOut,
            cancellationToken: cancellationToken);
}

static class AuthenticationManagerExtensions
{
    internal static ITelegramBotBuilder AddAuthenticationManager(this ITelegramBotBuilder builder)
    {
        builder.AddPersistence();
        builder.Services.TryAddScoped<AuthenticationManager>();
        builder.Services.AddMPlusClient();

        return builder;
    }
}
