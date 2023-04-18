using System.Security.Claims;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure;

public abstract class UpdateHandler : IHandler
{
    protected readonly TelegramBot Bot;
    protected readonly ILogger Logger;

    protected Update Update => Context.GetUpdate();
    protected ClaimsPrincipal User => Context.User;
    protected CancellationToken RequestAborted => Context.RequestAborted;
    protected HttpContext Context => _httpContextAccessor.HttpContext!;

    readonly IHttpContextAccessor _httpContextAccessor;

    protected UpdateHandler(TelegramBot bot, IHttpContextAccessor httpContextAccessor, ILogger logger)
    {
        Bot = bot;
        Logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public abstract Task HandleAsync();
}
