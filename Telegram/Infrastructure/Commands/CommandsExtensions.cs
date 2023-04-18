using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.Infrastructure.Middleware.UpdateRouting.UpdateTypeRouting;

namespace Telegram.Infrastructure.Commands;

static class CommandsExtensions
{
    internal static IServiceCollection AddCommandsCore(this IServiceCollection services)
        => services
        .AddScoped<IUpdateTypeRouter, MessageRouterMiddleware>()
        .AddScoped<MessageRouter, CommandRouterMiddleware>()
        .AddCommandParsing();

    internal static string CaseInsensitive(this string value) => value.ToLowerInvariant();
}
