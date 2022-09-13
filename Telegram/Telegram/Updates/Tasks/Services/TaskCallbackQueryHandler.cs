using Common;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Tasks.Models;

namespace Telegram.Telegram.Updates.Tasks.Services;

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
        var taskCallbackData = new TaskCallbackData(update.CallbackQuery!.Data!);

        if (taskCallbackData.Value.HasFlag(TaskQueryFlags.Details))
            await ShowDetailsAsync(chatId, taskCallbackData, authenticationToken);
    }

    async Task ShowDetailsAsync(ChatId chatId, TaskCallbackData taskCallbackData, ChatAuthenticationToken authenticationToken)
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
            await Bot.TrySendMessageAsync(chatId, "Couldn't get task details.");
    }
}
