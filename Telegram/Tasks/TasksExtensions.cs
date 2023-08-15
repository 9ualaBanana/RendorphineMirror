using GIBS;
using GIBS.CallbackQueries;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Tasks;
using Telegram.MPlus;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

static class TasksExtensions
{
    internal static ITelegramBotBuilder AddTasks(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddSingleton_<ICallbackQueryHandler, TaskCallbackQueryHandler>();
        builder
            .AddTaskDetails()
            .AddTasksCore();
        builder.Services.TryAddScoped<TelegramPreviewTaskResultHandler>();

        return builder;
    }

    internal static ITelegramBotBuilder AddTasksCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<TaskPrice>();
        builder.Services.TryAddScoped<TaskManager>();
        builder.Services.TryAddSingleton<OwnedRegisteredTasksCache>();
        builder.Services.AddMPlusClient();

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
