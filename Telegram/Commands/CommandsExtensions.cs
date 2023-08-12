using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Commands.Handlers;
using Telegram.Infrastructure;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;

namespace Telegram.Commands;

static class CommandsExtensions
{
    internal static ITelegramBotBuilder AddCommands(this ITelegramBotBuilder builder)
        => builder
        .AddCommandHandler<StartCommand>()
        .AddCommandHandler<LoginCommand>()
        .AddCommandHandler<LogoutCommand>()
        .AddCommandHandler<PromptCommand>()
        .AddCommandHandler<OnlineCommand>()
        .AddCommandHandler<OnlineCommand.Admin>()
        .AddCommandHandler<OfflineCommand>()
        .AddCommandHandler<OfflineCommand.Admin>()
        .AddCommandHandler<PingCommand>()
        .AddCommandHandler<PingCommand.Admin>()
        .AddCommandHandler<PingListCommand>()
        .AddCommandHandler<PingListCommand.Admin>()
        .AddCommandHandler<PaginatorCommand.Admin>()
        .AddCommandHandler<RemoveCommand>()
        .AddCommandHandler<PluginsCommand>()
        .AddCommandHandler<DeployCommand>();

    static ITelegramBotBuilder AddCommandHandler<TCommandHandler>(this ITelegramBotBuilder builder)
        where TCommandHandler : CommandHandler
    {
        builder.Services.TryAddScoped_<CommandHandler, TCommandHandler>();
        builder.Services.TryAddScoped<TCommandHandler>();

        return builder;
    }
}
