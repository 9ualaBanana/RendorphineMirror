using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Infrastructure.Messages;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

static class CommandsExtensions
{
    internal static ITelegramBotBuilder AddCommandsCore(this ITelegramBotBuilder builder)
    {
        builder.AddMessagesCore();
        builder.Services.TryAddScoped_<IMessageRouter, CommandRouterMiddleware>();
        builder.Services.TryAddScoped<Command.Factory>();
        builder.Services.TryAddSingleton<Command.Received>();
        builder.AddCommandsParsing();

        return builder;
    }


    internal static string CaseInsensitive(this string value) => value.ToLowerInvariant();
}
