using Microsoft.AspNetCore.Authorization;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Persistence;
using Telegram.Security.Authentication;
using Telegram.Security.Authorization;

namespace Telegram.Commands.Handlers;

public class LogoutCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly AuthenticationManager _authenticationManager;
    readonly TelegramBotDbContext _database;

    public LogoutCommand(
        AuthenticationManager authenticationManager,
        TelegramBotDbContext database,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LogoutCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _authenticationManager = authenticationManager;
        _database = database;
    }

    internal override Command Target => CommandFactory.Create("logout");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        if (await PersistedTelegramUser() is var user && user.IsAuthenticatedByMPlus)
        {
            _database.Remove(user.MPlusIdentity);
            await _database.SaveChangesAsync(RequestAborted);

            await _authenticationManager.SendSuccessfullLogOutMessageAsync(ChatId, RequestAborted);
        }


        async Task<TelegramBot.User.Entity> PersistedTelegramUser()
            => await _authenticationManager.PersistTelegramUserAsyncWith(Context.GetUpdate().ChatId(),
            save: false, Context.RequestAborted);
    }
}
