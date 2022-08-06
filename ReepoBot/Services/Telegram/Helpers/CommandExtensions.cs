namespace ReepoBot.Services.Telegram.Helpers;

internal static class CommandExtensions
{
    internal static IEnumerable<string> Arguments(this string command) => command.Split(null, 2).Last().Trim().Split();

    internal static IEnumerable<string> UnquotedArguments(this string command) =>
        command.Arguments().TakeWhile(argument => !argument.StartsWith('"'));

    internal static IEnumerable<string> QuotedArguments(this string command) =>
        command.Arguments()
            .SkipWhile(argument => !argument.StartsWith('"'))
            .Select(quotedArgument => quotedArgument.Trim('"'))
            .Where(argument => !string.IsNullOrWhiteSpace(argument));
}
