using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.Tasks;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static IServiceCollection AddTasks(this IServiceCollection services)
        => services
        .AddScoped<ICallbackQueryHandler, TaskCallbackQueryHandler>()
        .AddScoped<TelegramPreviewTaskResultHandler>()
        .AddTasksCore();
}
