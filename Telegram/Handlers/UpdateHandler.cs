using Telegram.Bot;

namespace Telegram.Handlers;

public abstract class UpdateHandler : IHttpContextHandler
{
	protected readonly TelegramBot Bot;
	protected readonly ILogger Logger;

	protected UpdateHandler(TelegramBot bot, ILogger logger)
	{
		Bot = bot;
		Logger = logger;
	}

    public abstract Task HandleAsync(HttpContext context);
}
