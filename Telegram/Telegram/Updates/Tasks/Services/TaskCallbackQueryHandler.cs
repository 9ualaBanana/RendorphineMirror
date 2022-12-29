using System.Text;
using Common.Tasks;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Tasks.Models;

namespace Telegram.Telegram.Updates.Tasks.Services;

public class TaskCallbackQueryHandler : AuthenticatedTelegramCallbackQueryHandlerBase
{
    public TaskCallbackQueryHandler(
        ILogger<TaskCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator) : base(logger, bot, authenticator)
    {
    }


    protected async override Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        var taskCallbackData = new TaskCallbackData(CallbackDataFrom(update));

        if (taskCallbackData.Value.HasFlag(TaskQueryFlags.Details))
            await ShowDetailsAsync(ChatIdFrom(update), taskCallbackData, authenticationToken);
    }

    async Task ShowDetailsAsync(ChatId chatId, TaskCallbackData taskCallbackData, ChatAuthenticationToken authenticationToken)
    {
        // Call /gettaskshard here (i.e. Apis.GetTaskShardAsync(string taskid, string? sessionId = default)) but better get it from some ShardRegistry to which that shard is added when its corresponding task is registered.
        var taskState = await Apis.GetTaskStateAsync(taskCallbackData.TaskId, authenticationToken.MPlus.SessionId);
        if (taskState)
        {
            var messageBuilder = new StringBuilder()
                .AppendLine($"TaskID: *{taskCallbackData.TaskId}*")
                .AppendLine($"State: *{taskState.Result.State}*")
                .AppendLine($"Progress: *{taskState.Result.Progress}*");
            if (taskState.Result.Times.Exist)
            {
                messageBuilder.AppendLine($"Duration: *{taskState.Result.Times.Total}*");
                messageBuilder.AppendLine($"Server: *{taskState.Result.Server}*");
            }

            await Bot.TrySendMessageAsync(chatId, messageBuilder.ToString());
        }
        else
            await Bot.TrySendMessageAsync(chatId, "Couldn't get task details.");
    }
}
