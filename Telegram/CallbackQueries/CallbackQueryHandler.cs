using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Handlers;
using Telegram.Models;

namespace Telegram.CallbackQueries;

/// <summary>
/// Service type of <see cref="CallbackQueryHandler{TCallbackQuery, ECallbackData}"/> implementations.
/// </summary>
/// <remarks>
/// <see cref="CallbackQueryHandler{TCallbackQuery, ECallbackData}"/> implementations must be registered with this interface
/// as their service type because closed generic types can't be registered as implementations of an open generic service type.
/// </remarks>
public interface ICallbackQueryHandler : IHttpContextHandler, ISwitchableService<ICallbackQueryHandler, string>
{
}

public abstract class CallbackQueryHandler<TCallbackQuery, ECallbackData> : UpdateHandler, ICallbackQueryHandler
    where TCallbackQuery : CallbackQuery<ECallbackData>, new()
    where ECallbackData : struct, Enum
{
    protected long ChatId => Update.CallbackQuery!.Message!.Chat.Id;

    protected readonly CallbackQuerySerializer Serializer;

    protected CallbackQueryHandler(
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
    {
        Serializer = serializer;
    }

    /// <summary>
    /// The last <typeparamref name="TCallbackQuery"/> matched via this method will be handled in a call to <see cref="HandleAsync(HttpContext)"/> that may follow.
    /// </summary>
    /// <param name="serializedCallbackQuery">
    /// Serialized <typeparamref name="TCallbackQuery"/> that may be handled in a call to <see cref="HandleAsync(HttpContext)"/> if this handler is appropriate for it.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this handler is appropriate for <paramref name="serializedCallbackQuery"/>; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches(string serializedCallbackQuery)
        => (_callbackQuery = Serializer.TryDeserialize<TCallbackQuery, ECallbackData>(serializedCallbackQuery)) is not null;

    TCallbackQuery? _callbackQuery;

    /// <summary>
    /// Handles the last <typeparamref name="TCallbackQuery"/> that matched this handler in a call to <see cref="Matches(string)"/> or throws an exception if none matched.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TCallbackQuery"/> hasn't matched this handler in a call to <see cref="Matches(string)"/> or that call didn't take place at all.
    /// </exception>
    public override async Task HandleAsync(HttpContext context)
    {
        if (_callbackQuery is not null)
        {
            await Bot.AnswerCallbackQueryAsync(Update.CallbackQuery!.Id, cancellationToken: context.RequestAborted);
            await HandleAsync(_callbackQuery, context);
        }
        else
        {
            var exception = new InvalidOperationException(
                $"{nameof(HandleAsync)} can be called only after this {nameof(CallbackQueryHandler<TCallbackQuery, ECallbackData>)} matched the callback query.",
                new ArgumentNullException(nameof(_callbackQuery))
                );
            Logger.LogCritical(exception, message: default);
            throw exception;
        }
    }

    public abstract Task HandleAsync(TCallbackQuery callbackQuery, HttpContext context);

    protected Task HandleUnknownCallbackData()
    {
        var exception = new ArgumentException($"Unknown {nameof(ECallbackData)}.");
        Logger.LogCritical(exception, message: default);
        throw exception;
    }
}
