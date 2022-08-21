using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Updates.Commands;
using ReepoBot.Services.Telegram.Updates.Images;

namespace ReepoBot.Services.Telegram.Updates;

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
