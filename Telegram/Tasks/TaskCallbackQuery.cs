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

    public override async Task HandleAsync(TaskCallbackQuery callbackQuery, HttpContext context)
        => await (callbackQuery.Data switch
        {
            TaskCallbackData.Details => ShowDetailsAsync(callbackQuery, context),
            _ => HandleUnknownCallbackData()
        });

    async Task ShowDetailsAsync(TaskCallbackQuery callbackQuery, HttpContext context)
    {
        var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(context.User));

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
                if (callbackQuery.Prototype!.Message is Message callbackQuerySource) // Only supports non-inline messages.
                    if (_taskDetailsMessagesCache.TryRetrieve(callbackQuerySource) is Message cachedTaskDetails)
                        await Bot.EditMessageAsync_(ChatId, cachedTaskDetails.MessageId, details, cancellationToken: context.RequestAborted);
                    else
                    {
                        Message taskDetails = await Bot.SendMessageAsync_(ChatId, details, cancellationToken: context.RequestAborted);
                        _taskDetailsMessagesCache.Add(callbackQuerySource, taskDetails);
                    }
                else
                {
                    var exception = new ArgumentNullException(nameof(CallbackQuery.Message),
                        $"{nameof(CallbackQuery)} {nameof(callbackQuery.Prototype)} must be originated from non-inline message.");
                    Logger.LogCritical(exception, message: default);
                    throw exception;
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
