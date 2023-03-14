using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.MPlus;

namespace Telegram.Tasks;

public class TaskCallbackQueryHandler : CallbackQueryHandler<TaskCallbackQuery, TaskCallbackData>
{
    public TaskCallbackQueryHandler(
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TaskCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
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
            var taskState = await api.GetTaskStateAsyncOrThrow(TaskApi.For(RegisteredTask.With(callbackQuery.TaskId), hostShard)).ThrowIfError();

            var messageBuilder = new StringBuilder()
                .AppendLine($"*Task ID* : `{callbackQuery.TaskId}`")
                .AppendLine($"*State* : `{taskState.State}`")
                .AppendLine($"*Progress* : `{taskState.Progress}`");
            if (taskState.Times.Exist)
            {
                messageBuilder.AppendLine($"*Duration* : `{taskState.Times.Total}`");
                messageBuilder.AppendLine($"*Server* : `{taskState.Server}`");
            }

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}

public record TaskCallbackQuery : CallbackQuery<TaskCallbackData>
{
    internal string TaskId => ArgumentAt(0).ToString()!;

    internal static TaskCallbackQuery DetailsFor(RegisteredTask registeredTask)
        => new Builder<TaskCallbackQuery>()
        .Data(TaskCallbackData.Details)
        .Arguments(registeredTask.Id)
        .Build();
}

[Flags]
public enum TaskCallbackData
{
    Details = 1
}
