using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.MPlus;
using Telegram.Tasks;

namespace Telegram.Infrastructure.Tasks;

static class TasksExtensions
{
    internal static ITelegramBotBuilder AddTasksCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<TaskPrice>();
        builder.Services.TryAddScoped<TaskManager>();
        builder.Services.TryAddSingleton<OwnedRegisteredTasksCache>();
        builder.Services.AddMPlusClient();

        return builder;
    }
}
