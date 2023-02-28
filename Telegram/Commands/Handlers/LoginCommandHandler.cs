using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Commands.SyntaxAnalysis;
using Telegram.Models;
using Telegram.Persistence;
using Telegram.Security.Authentication;

namespace Telegram.Commands.Handlers;

public class LoginCommandHandler : CommandHandler
{
    readonly MPlusClient _mPlusClient;
    readonly TelegramBotDbContext _database;

    public LoginCommandHandler(
        MPlusClient mPlusClient,
        TelegramBotDbContext database,
        TelegramBot bot,
        CommandParser parser,
        ILogger<LoginCommandHandler> logger) : base(bot, parser, logger)
    {
        _mPlusClient = mPlusClient;
        _database = database;
    }

    internal override Command Target => "login";

    protected override async Task HandleAsync(HttpContext context, ParsedCommand receivedCommand)
    {
        var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
        if (arguments.Length == 2)
        {
            if (await TryLogInAsyncUsing(arguments.First(), arguments.Last(), context) is MPlusIdentityEntity mPlusIdentityEntity)
                await TryPersistAuthenticatedUserAsync(mPlusIdentityEntity, context);
        }
        else await Bot.SendMessageAsync_(context.GetUpdate().ChatId(),
            $"Login must be performed like the following:\n" +
            $"`{Target.PrefixedCommandText} <email> <password>`",
            cancellationToken: context.RequestAborted);
    }

    async Task<MPlusIdentityEntity?> TryLogInAsyncUsing(string email, string password, HttpContext context)
    {
        try
        {
            return new MPlusIdentityEntity(
                await _mPlusClient.LogInAsyncUsing(email, password)
                );
        }
        catch (Exception ex)
        {
            await Bot.SendMessageAsync_(context.GetUpdate().ChatId(),
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
            await Bot.SendMessageAsync_(context.GetUpdate().ChatId(),
                "You are logged in now.",
                cancellationToken: context.RequestAborted);
        }
        else
        {
            await Bot.SendMessageAsync_(context.GetUpdate().ChatId(),
                "You need to logout first.",
                cancellationToken: context.RequestAborted);
        }


        async Task<bool> TryPersistAuthenticatedUserAsyncCore()
        {
            if (await _database.FindAsync<TelegramBotUserEntity>(context.GetUpdate().ChatId()) is TelegramBotUserEntity user)
            {
                if (user.MPlusIdentity is null)
                {
                    user.MPlusIdentity = mPlusIdentityEntity;
                    _database.Update(user);
                }
                else return false;
            }
            else
            { await _database.Users.AddAsync(new(context.GetUpdate().ChatId()) { MPlusIdentity = mPlusIdentityEntity }, context.RequestAborted); }

            await _database.SaveChangesAsync(context.RequestAborted);
            return true;
        }
    }
}
