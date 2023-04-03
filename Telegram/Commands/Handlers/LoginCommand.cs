using System.Collections.Immutable;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
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

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        var user = await _database.FindAsync<TelegramBotUserEntity>(ChatId);
        if (user is null)
        {
            user = (await _database.Users.AddAsync(new(ChatId), RequestAborted)).Entity;
            await _database.SaveChangesAsync(RequestAborted);
        }

        if (user.MPlusIdentity is null)
        {
            var arguments = receivedCommand.UnquotedArguments.ToImmutableArray();
            if (arguments.Length == 2)
            {
                if (await TryLogInAsyncUsing(arguments.First(), arguments.Last()) is MPlusIdentityEntity identity)
                {
                    await PersistAuthenticatedUserAsync(identity);
                    await SendSuccessfulLogInMessageAsync(identity.SessionId);
                }
            }
            else await Bot.SendMessageAsync_(ChatId,
                $"Login must be performed like the following:\n" +
                $"`{Target.PrefixedCommandText} <email> <password>`",
                cancellationToken: RequestAborted);
        }
        else await Bot.SendMessageAsync_(ChatId,
            $"You are already logged in.",
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

        async Task PersistAuthenticatedUserAsync(MPlusIdentityEntity identity)
        {
            user.MPlusIdentity = identity;
            _database.Update(user);
            await _database.SaveChangesAsync(RequestAborted);
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
