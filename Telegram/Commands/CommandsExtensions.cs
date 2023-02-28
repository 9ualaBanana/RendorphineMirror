using Telegram.Commands.Handlers;
using Telegram.Commands.SyntaxAnalysis;
using Telegram.Telegram.Updates.Commands;
using Telegram.Telegram.Updates.Commands.Offline;
using Telegram.Telegram.Updates.Commands.Online;
using Telegram.Telegram.Updates.Commands.Ping;
using Telegram.Telegram.Updates.Commands.Pinglist;
using Telegram.Telegram.Updates.Commands.Plugins;

namespace Telegram.Commands;

internal static class CommandsExtensions
{
    internal static string Command(this string command) => command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First().Trim();

    internal static IEnumerable<string> Arguments(this string command) =>
        command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last().Trim().Split()
        .TakeWhile(token => !token.StartsWith('/'));

    internal static IEnumerable<string> UnquotedArguments(this string command) =>
        command.Arguments().TakeWhile(argument => !argument.StartsWith('"'));

    internal static IEnumerable<string> QuotedArguments(this string command) =>
        command.Arguments()
            .SkipWhile(argument => !argument.StartsWith('"'))
            .Select(quotedArgument => quotedArgument.Trim('"'))
            .Where(argument => !string.IsNullOrWhiteSpace(argument));

    internal static string CaseInsensitive(this string value) => value.ToLowerInvariant();

    internal static IServiceCollection AddCommands(this IServiceCollection services)
        => services
        .AddCommandParsing()
        .AddCommandHandlers();

    static IServiceCollection AddCommandHandlers(this IServiceCollection services)
        => services
        .AddScoped<CommandHandler, LoginCommandHandler>()
        .AddScoped<CommandHandler, LogoutCommandHandler>();

    internal static IServiceCollection AddTelegramBotCommands(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<TelegramCommandHandler>()
            .AddScoped<Telegram.Updates.Commands.Command, LoginCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, PingListCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminPinglistCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, PingCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminPingCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, OnlineCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminOnlineCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, OfflineCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminOfflineCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, PluginsCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminPluginsCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, DeployCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, RemoveCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, LogoutCommand>()
            .AddScoped<Telegram.Updates.Commands.Command, AdminPaginatorCommand>();
}
