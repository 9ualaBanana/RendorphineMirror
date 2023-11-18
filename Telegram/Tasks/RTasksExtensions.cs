using GIBS;
using GIBS.CallbackQueries;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Localization;
using Telegram.MPlus;

namespace Telegram.Tasks;

static class RTasksExtensions
{
    internal static ITelegramBotBuilder AddRTasks(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddSingleton_<ICallbackQueryHandler, RTaskCallbackQueryHandler>();
        builder.AddRTasksCore();
        builder.Services.TryAddScoped<BotRTaskPreview>();

        return builder;
    }

    static ITelegramBotBuilder AddRTasksCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<RTaskManager, NotifyingRTaskManager>();
        builder.Services.AddTaskNotifications();
        builder.Services.TryAddSingleton<OwnedRegisteredTasksCache>();
        builder.Services.AddMPlusClient();

        return builder;
    }

    static IServiceCollection AddTaskNotifications(this IServiceCollection services)
    {
        services.TryAddSingleton<Notifications.RTask>();
        services.TryAddSingleton<Notifications.RTask.CachedMessages>();
        services.AddMemoryCache();

        return services;
    }
}
