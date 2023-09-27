using System.Text;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.MPlus.Security;

namespace Telegram.Tasks;

public class TaskCallbackQueryHandler : CallbackQueryHandler<TaskCallbackQuery, TaskCallbackData>
{
    readonly TaskDetails _taskDetails;

    public TaskCallbackQueryHandler(
        TaskDetails taskDetails,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TaskCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _taskDetails = taskDetails;
    }

    public override async Task HandleAsync(TaskCallbackQuery callbackQuery)
    {
        await (callbackQuery.Data switch
        {
            TaskCallbackData.Details => ShowDetailsAsync(),
            _ => HandleUnknownCallbackData()
        });


        async Task ShowDetailsAsync()
        {
            try { await ShowDetailsAsyncCore(); }
            catch { await Bot.SendMessageAsync_(ChatId, "Task details are currently unavailable."); }


            async Task ShowDetailsAsyncCore()
            {
                var taskState = await TaskApi.For(RegisteredTask.With(callbackQuery.TaskId)).With(MPlusIdentity.SessionIdOf(User)).GetStateAsync();
                await _taskDetails.SendAsync(ChatId, Details(), Message, RequestAborted);


                string Details()
                {
                    var details = new StringBuilder()
                        .AppendLine($"*Action* : `{callbackQuery.Action}`")
                        .AppendLine($"*Task ID* : `{callbackQuery.TaskId}`")
                        .AppendLine($"*State* : `{taskState.State}`");
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
