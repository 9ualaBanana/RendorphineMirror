using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Tasks;
using Telegram.Tasks.DetailsMessagesCache;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static IServiceCollection AddTasks(this IServiceCollection services)
        => services
        .AddSingleton<ICallbackQueryHandler, TaskCallbackQueryHandler>()
        .AddSingleton<TaskDetailsMessagesCache>()
        .AddScoped<TelegramPreviewTaskResultHandler>()
        .AddTasksCore();
}
