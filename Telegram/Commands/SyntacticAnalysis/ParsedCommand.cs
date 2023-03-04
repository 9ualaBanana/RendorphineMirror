namespace Telegram.Commands.SyntacticAnalysis;

public record ParsedCommand(Command Command, IEnumerable<string> UnquotedArguments, IEnumerable<string> QuotedArguments)
{
}
