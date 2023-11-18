using GIBS.CallbackQueries;
using GIBS.CallbackQueries.Serialization;
using Telegram.Localization;
using Telegram.MPlus.Security;

namespace Telegram.Tasks;

public class RTaskCallbackQueryHandler : CallbackQueryHandler<RTaskCallbackQuery, RTaskCallbackData>
{
    readonly Notifications.RTask _notifications;

    public RTaskCallbackQueryHandler(
        Notifications.RTask notifications,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RTaskCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _notifications = notifications;
    }

    public override async Task HandleAsync(RTaskCallbackQuery callbackQuery)
    {
        await (callbackQuery.Data switch
        {
            RTaskCallbackData.Details => ShowDetailsAsync(),
            _ => HandleUnknownCallbackData()
        });


        async Task ShowDetailsAsync()
        {
            try { await _notifications.SendDetailsAsync(ChatId, MPlusIdentity.SessionIdOf(User), callbackQuery, Message, RequestAborted); }
            catch { await _notifications.SendDetailsUnavailableAsync(ChatId, RequestAborted); }
        }
    }
}

public record RTaskCallbackQuery : CallbackQuery<RTaskCallbackData>
{
    internal string TaskId => ArgumentAt(0).ToString()!;
    internal string Action => ArgumentAt(1).ToString()!;
}

[Flags]
public enum RTaskCallbackData
{
    Details = 1
}
