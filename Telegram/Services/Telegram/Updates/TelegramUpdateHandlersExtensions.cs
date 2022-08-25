using Telegram.Services.Node;
using Telegram.Services.Telegram.Updates.Commands;
using Telegram.Services.Telegram.Updates.Images;

namespace Telegram.Services.Telegram.Updates;

public static class TelegramUpdateHandlersExtensions
{
    public static IServiceCollection AddTelegramUpdateHandlers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<TelegramUpdateHandler>()
            .AddScoped<TelegramMessageHandler>()
            .AddTelegramBotCommands()
            .AddTelegramImageProcessing()
            .AddScoped<TelegramCallbackQueryHandler>()
            .AddScoped<TelegramChatMemberUpdatedHandler>()
            .AddSingleton<NodeSupervisor>();
    }
}
