using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates;

namespace Telegram.Tasks.CallbackQuery;

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

        if (taskCallbackData.Value.HasFlag(TaskCallbackQueryFlags.Details))
            await ShowDetailsAsync(ChatIdFrom(update), taskCallbackData, authenticationToken);
    }

    async Task ShowDetailsAsync(ChatId chatId, TaskCallbackData taskCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var api = Apis.DefaultWithSessionId(authenticationToken.MPlus.SessionId);
        try
        {
            string shardHost = await api.GetTaskShardAsync(taskCallbackData.TaskId).ThrowIfError();
            var taskState = await api.GetTaskStateAsyncOrThrow(new ApiTask(taskCallbackData.TaskId) { HostShard = shardHost }).ThrowIfError();

            var messageBuilder = new StringBuilder()
                .AppendLine($"*Task ID*: `{taskCallbackData.TaskId}`")
                .AppendLine($"*State*: `{taskState.State}`")
                .AppendLine($"*Progress*: `{taskState.Progress}`");
            if (taskState.Times.Exist)
            {
                messageBuilder.AppendLine($"*Duration*: `{taskState.Times.Total}`");
                messageBuilder.AppendLine($"*Server*: `{taskState.Server}`");
            }

            await Bot.SendMessageAsync_(chatId, messageBuilder.ToString());
        }
        catch { await Bot.SendMessageAsync_(chatId, "Task details are unavailable."); }
    }
}
