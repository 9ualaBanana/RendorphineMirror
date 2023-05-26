using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

static class CommandsExtensions
{
    internal static ITelegramBotBuilder AddCommandsCore(this ITelegramBotBuilder builder)
    {
        builder
            .AddMessageRouter<CommandRouterMiddleware>()
            
            .Services
            .AddSingleton<Command.Received>()
            .AddScoped<Command.Factory>()
            .AddCommandsParsing();
        return builder;
    }


    internal static string CaseInsensitive(this string value) => value.ToLowerInvariant();
}
