using Telegram.Services.Node;
using Telegram.Services.Telegram.Updates.Commands;
using Telegram.Telegram.Updates.Images;

namespace Telegram.Telegram.Updates;

public static class TelegramUpdateHandlersExtensions
{
    public static IServiceCollection AddTelegramUpdateHandlers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<TelegramUpdateTypeHandler>()
            .AddScoped<TelegramMessageHandler>()
            .AddTelegramBotCommands()
            .AddTelegramImageProcessing()
            .AddScoped<TelegramCallbackQueryHandler>()
            .AddScoped<TelegramChatMemberUpdatedHandler>()
            .AddSingleton<UserNodes>();
    }
}
