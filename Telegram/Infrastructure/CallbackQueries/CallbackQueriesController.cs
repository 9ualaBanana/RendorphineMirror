using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure.CallbackQueries;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class CallbackQueriesController : ControllerBase
{
    internal const string PathFragment = "callback_query";

    readonly ILogger _logger;

    public CallbackQueriesController(ILogger<CallbackQueriesController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task Handle([FromServices] IEnumerable<ICallbackQueryHandler> callbackQueryHandlers)
    {
        Exception? exception = null;

        var callbackQuery = HttpContext.GetUpdate().CallbackQuery!;
        if (callbackQuery.Data is not null)
            if (callbackQueryHandlers.Switch(callbackQuery) is ICallbackQueryHandler callbackQueryHandler)
            { await callbackQueryHandler.HandleAsync(); return; }

            else exception = new NotImplementedException(
                $"None of registered implementations of {nameof(ICallbackQueryHandler)} matched received callback query: {callbackQuery.Data}"
                );
        else exception = new ArgumentNullException(
            $"{nameof(CallbackQuery.Data)} must contain a serialized {typeof(CallbackQuery<>)}"
            );

        _logger.LogCritical(exception, message: default);
        throw exception;
    }
}
