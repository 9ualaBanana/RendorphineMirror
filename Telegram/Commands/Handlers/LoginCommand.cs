using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

/// <remarks>
/// Each invocation of <see cref="LoginCommand"/> results in persisting <see cref="ChatId"/>
/// of the user who invoked it, if hasn't been persisted yet. Resulting <see cref="TelegramBotUserEntity"/>
/// will contain <see cref="TelegramBotUserEntity.MPlusIdentity"/> if the user is already logged in.
/// Otherwise, a login attempt is made using credentials provided by the user and 
/// </remarks>
public class LoginCommand : CommandHandler
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBotDbContext _database;

    public LoginCommand(
        MPlusClient mPlusClient,
        TelegramBotDbContext database,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LoginCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _mPlusClient = mPlusClient;
        _database = database;
    }

    internal override Command Target => CommandFactory.Create("login");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        // `save: true` persists ChatId of the current user even if M+ authentication fails.
        var user = await PersistTelegramUserAsync(save: true);

        if (user.MPlusIdentity is null)
            await TryLogInAsync(user);
        else await Bot.SendMessageAsync_(ChatId,
            $"You are already logged in.",
            cancellationToken: RequestAborted);


        async Task<TelegramBotUserEntity> PersistTelegramUserAsync(bool save = false)
        {
            var user = await _database.FindAsync<TelegramBotUserEntity>(ChatId);
            if (user is null)
            {
                user = (await _database.Users.AddAsync(new(ChatId), RequestAborted)).Entity;
                if (save) await _database.SaveChangesAsync(RequestAborted);
            }
            return user;
        }

        async Task TryLogInAsync(TelegramBotUserEntity user)
        {
            var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
            if (arguments.Length == 2)
            {
                if (await TryLogInAsyncUsing(arguments.First(), arguments.Last()) is MPlusIdentityEntity identity)
                {
                    await PersistMPlusUserIdentityAsync(user, identity, save: true);
                    await SendSuccessfulLogInMessageAsync(identity.SessionId);
                }
            }
            else await Bot.SendMessageAsync_(ChatId,
                $"Login must be performed like the following:\n" +
                $"`{Target.Prefixed} <email> <password>`",
                cancellationToken: RequestAborted);


            async Task<MPlusIdentityEntity?> TryLogInAsyncUsing(string email, string password)
            {
                try { return new MPlusIdentityEntity(await _mPlusClient.TaskManager.LogInAsyncUsing(email, password)); }
                catch (Exception ex)
                {
                    await Bot.SendMessageAsync_(ChatId,
                        "Login attempt failed:\n" +
                        ex.Message,
                        cancellationToken: RequestAborted);
                    return null;
                }
            }

            async Task PersistMPlusUserIdentityAsync(TelegramBotUserEntity user, MPlusIdentityEntity identity, bool save = false)
            {
                user.MPlusIdentity = identity;
                _database.Update(user);
                if (save) await _database.SaveChangesAsync(RequestAborted);
            }

            async Task SendSuccessfulLogInMessageAsync(string sessionId)
            {
                var balance = await _mPlusClient.TaskLauncher.RequestBalanceAsync(sessionId, RequestAborted);
                await Bot.SendMessageAsync_(Update.ChatId(),
                    "You are logged in now.\n\n" +
                    $"*Balance* : `{balance.RealBalance}`",
                    cancellationToken: RequestAborted);
            }
        }
    }
}
