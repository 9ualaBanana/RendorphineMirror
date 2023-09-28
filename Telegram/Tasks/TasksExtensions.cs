using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles;
using Telegram.MPlus;

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
        builder.Services.TryAddScoped<BotRTaskPreview>();

        return builder;
    }

    static ITelegramBotBuilder AddTasksCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<TaskPrice>();
        builder.Services.TryAddScoped<RTask>();
        builder.Services.TryAddScoped<BotRTask>();
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
