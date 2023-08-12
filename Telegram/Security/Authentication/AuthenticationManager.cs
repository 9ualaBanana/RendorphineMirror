using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Localization.Resources;
using Telegram.MPlus;
using Telegram.MPlus.Clients;
using Telegram.Persistence;

namespace Telegram.Security.Authentication;

public class AuthenticationManager
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot _bot;
	readonly TelegramBotDbContext _database;
    readonly LocalizedText.Authentication _localizedAuthenticationText;

	public AuthenticationManager(
        MPlusClient mPlusClient,
        TelegramBot bot,
        TelegramBotDbContext database,
        LocalizedText.Authentication localizedAuthenticationMessage)
	{
        _mPlusClient = mPlusClient;
        _bot = bot;
		_database = database;
        _localizedAuthenticationText = localizedAuthenticationMessage;
	}

    internal async Task TryAuthenticateByMPlusAsync(TelegramBotUserEntity user, string email, string password, CancellationToken cancellationToken)
    {
        if (await TryAuthenticateByMPlusAsync() is MPlusIdentityEntity identity)
        {
            await AddMPlusIdentityAsync(user, identity, cancellationToken);
            await _bot.SendMessageAsync_(user.ChatId,
                await _localizedAuthenticationText.SuccessfulLogInAsync(user.ChatId, user.MPlusIdentity!, cancellationToken),
                cancellationToken: cancellationToken);
        }


        async Task<MPlusIdentityEntity?> TryAuthenticateByMPlusAsync()
        {
            try { return new MPlusIdentityEntity(await _mPlusClient.TaskManager.AuthenticateAsyncUsing(email, password)); }
            catch (Exception ex)
            {
                await _bot.SendMessageAsync_(user.ChatId,
                    _localizedAuthenticationText.FailedLogIn(ex),
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
    internal async ValueTask<TelegramBotUserEntity> GetBotUserAsyncWith(ChatId chatId)
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
        TelegramBotUserEntity user,
        MPlusIdentityEntity identity,
        CancellationToken cancellationToken)
    {
        user.MPlusIdentity = identity;
        _database.Update(user);

        await _database.SaveChangesAsync(cancellationToken);
    }
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
