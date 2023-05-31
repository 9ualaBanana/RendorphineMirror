using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure;
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
        builder.AddCallbackQueries();
        builder.Services.TryAddSingleton_<ICallbackQueryHandler, TaskCallbackQueryHandler>();
        builder
            .AddTaskDetails()
            .AddTasksCore()
            .AddMediaFilesCore();
        builder.Services.TryAddScoped<TelegramPreviewTaskResultHandler>();

        return builder;
    }

    static ITelegramBotBuilder AddTaskDetails(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddSingleton<TaskDetails>();
        builder.Services.TryAddSingleton<TaskDetails.CachedMessages>();
        builder.Services.AddMemoryCache();

        return builder;
    }
}
