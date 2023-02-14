namespace Telegram.Commands.SyntaxAnalysis;

public record ParsedCommand(Command Command, IEnumerable<string> UnquotedArguments, IEnumerable<string> QuotedArguments)
{
}
