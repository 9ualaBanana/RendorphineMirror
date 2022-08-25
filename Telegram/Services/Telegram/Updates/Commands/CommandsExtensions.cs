namespace Telegram.Services.Telegram.Updates.Commands;

internal static class CommandsExtensions
{
    internal static string Command(this string command) => command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First().Trim();

    internal static IEnumerable<string> Arguments(this string command) => command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last().Trim().Split();

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
            .AddScoped<Command, PingCommand>()
            .AddScoped<Command, OnlineCommand>()
            .AddScoped<Command, OfflineCommand>()
            .AddScoped<Command, PluginsCommand>()
            .AddScoped<Command, ProcessCommand>()
            .AddScoped<Command, DeployCommand>()
            .AddScoped<Command, RemoveCommand>()
            .AddScoped<Command, LogoutCommand>();
}
