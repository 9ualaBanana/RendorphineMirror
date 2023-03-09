using Telegram.Commands.Handlers;
using Telegram.Commands.SyntacticAnalysis;

namespace Telegram.Commands;

internal static class CommandsExtensions
{
    internal static string CaseInsensitive(this string value) => value.ToLowerInvariant();

    internal static IServiceCollection AddCommands(this IServiceCollection services)
        => services
        .AddCommandParsing()
        .AddCommandHandlers();

    static IServiceCollection AddCommandHandlers(this IServiceCollection services)
        => services
        .AddScoped<CommandHandler, LoginCommand>()
        .AddScoped<CommandHandler, LogoutCommand>()
        .AddScoped<CommandHandler, OnlineCommand>()
        .AddScoped<CommandHandler, OnlineCommand.Admin>()
        .AddScoped<CommandHandler, OfflineCommand>()
        .AddScoped<CommandHandler, OfflineCommand.Admin>()
        .AddScoped<CommandHandler, PingCommand>()
        .AddScoped<CommandHandler, PingCommand.Admin>()
        .AddScoped<CommandHandler, PingListCommand>()
        .AddScoped<CommandHandler, PingListCommand.Admin>()
        .AddScoped<CommandHandler, PaginatorCommand.Admin>()
        .AddScoped<CommandHandler, RemoveCommand>()
        .AddScoped<CommandHandler, PluginsCommand>()
        .AddScoped<CommandHandler, DeployCommand>();
}
