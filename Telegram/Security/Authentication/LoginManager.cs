using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.MPlus;
using Telegram.Persistence;

namespace Telegram.Security.Authentication;

public class LoginManager
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot _bot;
	readonly TelegramBotDbContext _database;

	public LoginManager(MPlusClient mPlusClient, TelegramBot bot, TelegramBotDbContext database)
	{
        _mPlusClient = mPlusClient;
        _bot = bot;
		_database = database;
	}

    internal async Task PersistMPlusUserIdentityAsync(
        TelegramBotUserEntity user,
        MPlusIdentityEntity identity,
        bool save,
        CancellationToken cancellationToken)
    {
        user.MPlusIdentity = identity;
        _database.Update(user);

        if (save) await _database.SaveChangesAsync(cancellationToken);
    }

    internal async Task<TelegramBotUserEntity> PersistTelegramUserAsync(
        ChatId chatId,
        bool save,
        CancellationToken cancellationToken)
    {
        var user = await _database.FindOrAddUserAsyncWith(chatId, cancellationToken);

        if (save) await _database.SaveChangesAsync(cancellationToken);

        return user;
    }

    internal async Task TryLogInAsync(TelegramBotUserEntity user, string email, string password, CancellationToken cancellationToken)
    {
        if (await TryLogInAsync() is MPlusIdentityEntity identity)
        {
            await PersistMPlusUserIdentityAsync(user, identity, save: true, cancellationToken);
            await SendSuccessfulLogInMessageAsync(user.ChatId, identity.SessionId, cancellationToken);
        }


        async Task<MPlusIdentityEntity?> TryLogInAsync()
        {
            try { return new MPlusIdentityEntity(await _mPlusClient.TaskManager.LogInAsyncUsing(email, password)); }
            catch (Exception ex)
            {
                await _bot.SendMessageAsync_(user.ChatId,
                    "Login attempt failed:\n" +
                ex.Message,
                    cancellationToken: cancellationToken);
                return null;
            }
        }
    }

    internal async Task SendSuccessfulLogInMessageAsync(ChatId chatId, string sessionId, CancellationToken cancellationToken)
    {
        var balance = await _mPlusClient.TaskLauncher.RequestBalanceAsync(sessionId, cancellationToken);
        await _bot.SendMessageAsync_(chatId,
            "You are logged in now.\n\n" +
            $"*Balance* : `{balance.RealBalance}`",
            cancellationToken: cancellationToken);
    }
}
