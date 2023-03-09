using Telegram.CallbackQueries;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static IServiceCollection AddTasks(this IServiceCollection services)
        => services
        .AddCallbackQueries()
        .AddSingleton<OwnedRegisteredTasksCache>()
        .AddScoped<ICallbackQueryHandler, TaskCallbackQueryHandler>();
}
