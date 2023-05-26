using Telegram.Infrastructure.Bot;
using Telegram.MPlus;
using Telegram.Tasks;

namespace Telegram.Infrastructure.Tasks;

static class TasksExtensions
{
    internal static ITelegramBotBuilder AddTasksCore(this ITelegramBotBuilder builder)
    {
        builder.Services
            .AddScoped<TaskPrice>()
            .AddSingleton<OwnedRegisteredTasksCache>()
            .AddMPlusClient();
        return builder;
    }
}
