using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.MPlus.Security;

namespace Telegram.Tasks;

public class TaskCallbackQueryHandler : CallbackQueryHandler<TaskCallbackQuery, TaskCallbackData>
{
    readonly TaskDetails _taskDetailsManager;

    public TaskCallbackQueryHandler(
        TaskDetails taskDetailsManager,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TaskCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _taskDetailsManager = taskDetailsManager;
    }

    public override async Task HandleAsync(TaskCallbackQuery callbackQuery)
        => await (callbackQuery.Data switch
        {
            TaskCallbackData.Details => ShowDetailsAsync(callbackQuery),
            _ => HandleUnknownCallbackData()
        });

    async Task ShowDetailsAsync(TaskCallbackQuery callbackQuery)
    {
        try { await ShowDetailsAsyncCore(); }
        catch { await Bot.SendMessageAsync_(ChatId, "Task details are currently unavailable."); }


        async Task ShowDetailsAsyncCore()
        {
            var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(User));
            string hostShard = await api.GetTaskShardAsync(callbackQuery.TaskId).ThrowIfError();
            var taskState = await api.GetTaskStateAsyncOrThrow(TaskApi.For(RegisteredTask.With(callbackQuery.TaskId))).ThrowIfError();

            await _taskDetailsManager.SendAsync(ChatId, Details(), Message, RequestAborted);


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
