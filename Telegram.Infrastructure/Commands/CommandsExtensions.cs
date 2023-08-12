using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;

namespace Telegram.Infrastructure.Commands;

static class CommandsExtensions
{
    internal static ITelegramBotBuilder AddCommandsCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped_<IMessageRouter, CommandRouterMiddleware>();
        builder.Services.TryAddScoped<Command.Factory>();
        builder.Services.TryAddSingleton<Command.Received>();
        builder.AddCommandsParsing();

        return builder;
    }
}
