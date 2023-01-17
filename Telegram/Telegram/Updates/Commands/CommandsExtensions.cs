using Telegram.Telegram.Updates.Commands.Offline;
using Telegram.Telegram.Updates.Commands.Online;
using Telegram.Telegram.Updates.Commands.Ping;
using Telegram.Telegram.Updates.Commands.Pinglist;
using Telegram.Telegram.Updates.Commands.Plugins;

namespace Telegram.Telegram.Updates.Commands;

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

    internal static IServiceCollection AddTelegramBotCommands(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddScoped<TelegramCommandHandler>()
            .AddScoped<Command, LoginCommand>()
            .AddScoped<Command, PingListCommand>()
            .AddScoped<Command, AdminPinglistCommand>()
            .AddScoped<Command, PingCommand>()
            .AddScoped<Command, AdminPingCommand>()
            .AddScoped<Command, OnlineCommand>()
            .AddScoped<Command, AdminOnlineCommand>()
            .AddScoped<Command, OfflineCommand>()
            .AddScoped<Command, AdminOfflineCommand>()
            .AddScoped<Command, PluginsCommand>()
            .AddScoped<Command, AdminPluginsCommand>()
            .AddScoped<Command, DeployCommand>()
            .AddScoped<Command, RemoveCommand>()
            .AddScoped<Command, LogoutCommand>()
            .AddScoped<Command, AdminPaginatorCommand>();
}
