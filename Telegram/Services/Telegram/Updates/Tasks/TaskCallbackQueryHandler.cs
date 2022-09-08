using Common;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Telegram.Updates.Tasks;

public class TaskCallbackQueryHandler : AuthenticatedTelegramUpdateHandler
{
    public TaskCallbackQueryHandler(
        ILogger<TaskCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator) : base(logger, bot, authenticator)
    {
    }



    protected async override Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var taskCallbackData = new TaskCallbackData(update.CallbackQuery.Data!);

        if (taskCallbackData.Value.HasFlag(TaskQueryFlags.Details))
        {
            var taskState = await Apis.GetTaskStateAsync(taskCallbackData.TaskId, authenticationToken.MPlus.SessionId);
            if (taskState)
            {
                var messageBuilder = new StringBuilder()
                    .AppendLine($"TaskID: *{taskCallbackData.TaskId}*")
                    .AppendLine($"State: *{taskState.Result.State}*")
                    .AppendLine($"Progress: *{taskState.Result.Progress}*");
                if (taskState.Result.Times.Exist)
                    messageBuilder.AppendLine($"Duration: *{taskState.Result.Times.Total}*");
                messageBuilder.AppendLine($"Server: *{taskState.Result.Server}*");

                await Bot.TrySendMessageAsync(chatId, messageBuilder.ToString());
            }
            else
                await Bot.TrySendMessageAsync(chatId, "Couldn't get task progress.");
        }
    }
}
