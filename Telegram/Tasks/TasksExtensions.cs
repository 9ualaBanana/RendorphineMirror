using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static ITelegramBotBuilder AddTasks(this ITelegramBotBuilder builder)
    {
        builder
            .AddTasksCore()
            .AddMediaFilesCore()
            .AddCallbackQueries()

            .Services
            .AddSingleton<ICallbackQueryHandler, TaskCallbackQueryHandler>()
            .AddTaskDetails()
            .AddScoped<TelegramPreviewTaskResultHandler>();
        return builder;
    }

    static IServiceCollection AddTaskDetails(this IServiceCollection services)
        => services.AddSingleton<TaskDetails>().AddSingleton<TaskDetails.CachedMessages>().AddMemoryCache();
}
