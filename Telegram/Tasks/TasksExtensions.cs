using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Tasks;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static IServiceCollection AddTasks(this IServiceCollection services)
        => services
        .AddSingleton<ICallbackQueryHandler, TaskCallbackQueryHandler>()
        .AddSingleton<TaskDetails>().AddSingleton<TaskDetails.CachedMessages>().AddMemoryCache()
        .AddScoped<TelegramPreviewTaskResultHandler>()
        .AddTasksCore();
}
