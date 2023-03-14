using Telegram.Infrastructure.CallbackQueries;
namespace Telegram.Infrastructure.Tasks;

static class TasksExtensions
{
    internal static IServiceCollection AddTasksCore(this IServiceCollection services)
        => services
        .AddSingleton<OwnedRegisteredTasksCache>()
        .AddCallbackQueries();
}
