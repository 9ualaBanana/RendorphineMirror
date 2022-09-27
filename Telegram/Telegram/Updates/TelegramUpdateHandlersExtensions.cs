using Telegram.Services.Node;
using Telegram.Services.Telegram.Updates.Commands;
using Telegram.Telegram.MessageChunker.Services;
using Telegram.Telegram.Updates.Images;

namespace Telegram.Telegram.Updates;

public static class TelegramUpdateHandlersExtensions
{
    public static IServiceCollection AddTelegramUpdateHandlers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<TelegramUpdateTypeHandler>()
            .AddScoped<TelegramMessageHandler>().AddSingleton<TextChunker>()
            .AddTelegramBotCommands()
            .AddTelegramImageProcessing()
            .AddScoped<TelegramCallbackQueryHandler>()
            .AddScoped<TelegramChatMemberUpdatedHandler>()
            .AddSingleton<UserNodes>();
    }
}
