using Common;
using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Tasks;

public class TaskCallbackQueryHandler : AuthenticatedTelegramUpdateHandler
{
    public TaskCallbackQueryHandler(
        ILogger<TaskCallbackQueryHandler> logger,
        TelegramBot bot,
        TelegramChatIdAuthenticator authenticator) : base(logger, bot, authenticator)
    {
    }



    protected async override Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var taskCallbackData = new TaskCallbackData(update.CallbackQuery.Data!);

        if (taskCallbackData.Value.HasFlag(TaskQueryFlags.Details))
        {
            var taskState = await Apis.GetTaskStateAsync(taskCallbackData.TaskId, authenticationToken.SessionId);
            if (taskState)
            {
                await Bot.TrySendMessageAsync(chatId, $"TaskID: *{taskCallbackData.TaskId}*\nState: *{taskState.Result.State}*\nProgress: *{taskState.Result.Progress}*\nServer: *{taskState.Result.Server}*");
            }
            else
            {
                await Bot.TrySendMessageAsync(chatId, "Couldn't get task progress.");
            }
        }
    }
}
