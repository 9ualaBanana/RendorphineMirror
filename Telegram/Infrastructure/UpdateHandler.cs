using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure;

public abstract class UpdateHandler : IHttpContextHandler
{
    protected readonly TelegramBot Bot;
    protected readonly ILogger Logger;

    protected Update Update => _httpContextAccessor.HttpContext!.GetUpdate();

    readonly IHttpContextAccessor _httpContextAccessor;

    protected UpdateHandler(TelegramBot bot, IHttpContextAccessor httpContextAccessor, ILogger logger)
    {
        Bot = bot;
        _httpContextAccessor = httpContextAccessor;
        Logger = logger;
    }

    public abstract Task HandleAsync(HttpContext context);
}
