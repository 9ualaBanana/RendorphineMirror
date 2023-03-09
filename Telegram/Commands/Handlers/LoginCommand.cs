using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.MPlus;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

public class LoginCommand : CommandHandler
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBotDbContext _database;

    public LoginCommand(
        MPlusClient mPlusClient,
        TelegramBotDbContext database,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LoginCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _mPlusClient = mPlusClient;
        _database = database;
    }

    internal override Command Target => "login";

    protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
    {
        var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
        if (arguments.Length == 2)
        {
            if (await TryLogInAsyncUsing(arguments.First(), arguments.Last(), context) is MPlusIdentityEntity mPlusIdentityEntity)
                await TryPersistAuthenticatedUserAsync(mPlusIdentityEntity, context);
        }
        else await Bot.SendMessageAsync_(Update.ChatId(),
            $"Login must be performed like the following:\n" +
            $"`{Target.PrefixedCommandText} <email> <password>`",
            cancellationToken: context.RequestAborted);
    }

    async Task<MPlusIdentityEntity?> TryLogInAsyncUsing(string email, string password, HttpContext context)
    {
        try { return new MPlusIdentityEntity(await _mPlusClient.TaskManager.LogInAsyncUsing(email, password)); }
        catch (Exception ex)
        {
            await Bot.SendMessageAsync_(Update.ChatId(),
                "Login failed:\n" +
                ex.Message,
                cancellationToken: context.RequestAborted);
            return null;
        }
    }

    async Task TryPersistAuthenticatedUserAsync(MPlusIdentityEntity mPlusIdentityEntity, HttpContext context)
    {
        if (await TryPersistAuthenticatedUserAsyncCore())
        {
            await Bot.SendMessageAsync_(Update.ChatId(),
                "You are logged in now.",
                cancellationToken: context.RequestAborted);
        }
        else
        {
            await Bot.SendMessageAsync_(Update.ChatId(),
                "You need to logout first.",
                cancellationToken: context.RequestAborted);
        }


        async Task<bool> TryPersistAuthenticatedUserAsyncCore()
        {
            if (await _database.FindAsync<TelegramBotUserEntity>(Update.ChatId()) is TelegramBotUserEntity user)
            {
                if (user.MPlusIdentity is null)
                {
                    user.MPlusIdentity = mPlusIdentityEntity;
                    _database.Update(user);
                }
                else return false;
            }
            else
            { await _database.Users.AddAsync(new(Update.ChatId()) { MPlusIdentity = mPlusIdentityEntity }, context.RequestAborted); }

            await _database.SaveChangesAsync(context.RequestAborted);
            return true;
        }
    }
}
