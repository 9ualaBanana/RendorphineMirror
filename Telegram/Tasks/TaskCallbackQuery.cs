using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.MPlus;
using Telegram.Tasks.DetailsMessagesCache;

namespace Telegram.Tasks;

public class TaskCallbackQueryHandler : CallbackQueryHandler<TaskCallbackQuery, TaskCallbackData>
{
    readonly TaskDetailsMessagesCache _taskDetailsMessagesCache;

    public TaskCallbackQueryHandler(
        TaskDetailsMessagesCache taskDetailsMessagesCache,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TaskCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _taskDetailsMessagesCache = taskDetailsMessagesCache;
    }

    public override async Task HandleAsync(TaskCallbackQuery callbackQuery)
        => await (callbackQuery.Data switch
        {
            TaskCallbackData.Details => ShowDetailsAsync(callbackQuery),
            _ => HandleUnknownCallbackData()
        });

    async Task ShowDetailsAsync(TaskCallbackQuery callbackQuery)
    {
        var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(User));

        try { await ShowDetailsAsyncCore(); }
        catch { await Bot.SendMessageAsync_(ChatId, "Task details are currently unavailable."); }


        async Task ShowDetailsAsyncCore()
        {
            string hostShard = await api.GetTaskShardAsync(callbackQuery.TaskId).ThrowIfError();
            var taskState = await api.GetTaskStateAsyncOrThrow(TaskApi.For(RegisteredTask.With(callbackQuery.TaskId))).ThrowIfError();

            var details = Details();
            await SendDetailsMessageAsync();


            string Details()
            {
                var details = new StringBuilder()
                    .AppendLine($"*Action* : `{callbackQuery.Action}`")
                    .AppendLine($"*Task ID* : `{callbackQuery.TaskId}`")
                    .AppendLine($"*State* : `{taskState.State}`")
                    .AppendLine($"*Progress* : `{taskState.Progress}`");
                if (taskState.Times.Exist)
                {
                    details.AppendLine($"*Duration* : `{taskState.Times.Total}`");
                    details.AppendLine($"*Server* : `{taskState.Server}`");
                }
                return details.ToString();
            }

            async Task SendDetailsMessageAsync()
            {
                if (_taskDetailsMessagesCache.TryRetrieveBy(Message) is Message cachedTaskDetails)
                    await Bot.EditMessageAsync_(ChatId, cachedTaskDetails.MessageId, details, cancellationToken: RequestAborted);
                else
                {
                    Message taskDetails = await Bot.SendMessageAsync_(ChatId, details, cancellationToken: RequestAborted);
                    _taskDetailsMessagesCache.Add(Message, taskDetails);
                }
            }
        }
    }
}

public record TaskCallbackQuery : CallbackQuery<TaskCallbackData>
{
    internal string TaskId => ArgumentAt(0).ToString()!;
    internal string Action => ArgumentAt(1).ToString()!;
}

[Flags]
public enum TaskCallbackData
{
    Details = 1
}
