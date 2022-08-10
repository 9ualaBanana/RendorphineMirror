using ReepoBot.Services.Node;
using ReepoBot.Services.Telegram.Updates.Commands;

namespace ReepoBot.Services.Telegram.Updates;

public static class TelegramUpdateHandlersExtensions
{
    public static IServiceCollection AddTelegramUpdateHandlers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<TelegramUpdateHandler>()
            .AddScoped<TelegramMessageHandler>()
            .AddScoped<TelegramCommandHandler>()
            .AddScoped<TelegramChatMemberUpdatedHandler>()
            .AddSingleton<NodeSupervisor>();
    }
}
