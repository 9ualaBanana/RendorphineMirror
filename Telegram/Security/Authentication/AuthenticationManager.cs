using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Localization.Resources;
using Telegram.MPlus.Clients;
using Telegram.Persistence;

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

    internal async Task TryAuthenticateByMPlusAsync(TelegramBotUserEntity user, string email, string password, CancellationToken cancellationToken)
    {
        if (await TryAuthenticateByMPlusAsync() is MPlusIdentityEntity identity)
        {
            await PersistMPlusUserIdentityAsync(user, identity, cancellationToken);
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
    /// Persists <see cref="TelegramBotUserEntity"/> with provided <paramref name="chatId"/> if it is not persisted yet
    /// or simplly returns the persisted <see cref="TelegramBotUserEntity"/> from the database.
    /// </summary>
    /// <returns>
    /// Persisted <see cref="TelegramBotUserEntity"/> who might be not logged in
    /// (i.e. its <see cref="TelegramBotUserEntity.MPlusIdentity"/> might be <see langword="null"/>.
    /// </returns>
    internal async Task<TelegramBotUserEntity> PersistTelegramUserAsyncWith(
        ChatId chatId,
        bool save,
        CancellationToken cancellationToken)
    {
        var user = await _database.FindAsync<TelegramBotUserEntity>(chatId) ??
            (await _database.Users.AddAsync(new(chatId), cancellationToken)).Entity;

        if (save) await _database.SaveChangesAsync(cancellationToken);

        return user;
    }

    internal async Task PersistMPlusUserIdentityAsync(
        TelegramBotUserEntity user,
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
            _localiedAuthenticationMessage.Success(balance.RealBalance),
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
