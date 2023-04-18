using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.SyntacticAnalysis;

/// <summary>
/// Exposes <see cref="TryParse(string)"/> method that returns <see cref="ParsedCommand"/>
/// if the input <see cref="string"/> was a syntactically correct command.
/// </summary>
public class CommandParser
{
    readonly Tokenizer<CommandToken_> _tokenizer;

    public CommandParser(Tokenizer<CommandToken_> tokenizer)
    {
        _tokenizer = tokenizer;
    }

    /// <summary>
    /// Uses <see cref="Tokenizer{TToken}"/> to make an attempt in constructing <see cref="ParsedCommand"/>
    /// from <paramref name="rawCommand"/> if it is a syntactically correct command.
    /// </summary>
    /// <param name="rawCommand"><see cref="string"/> that should represent a command.</param>
    /// <returns>
    /// <see cref="ParsedCommand"/> if <paramref name="rawCommand"/> is a syntactically correct command.
    /// </returns>
    internal ParsedCommand? TryParse(string rawCommand)
    {
        var tokens = _tokenizer.Tokenize(rawCommand).GetEnumerator();

        if (tokens.MoveNext() && tokens.Current is CommandToken commandToken)
        {
            var command = Command.From(commandToken);

            List<Token> arguments = new();
            while (tokens.MoveNext())
                if (tokens.Current is UnquotedCommandArgumentToken || tokens.Current is QuotedCommandArgumentToken)
                    arguments.Add(tokens.Current);
            var unquotedArguments = arguments.OfType<UnquotedCommandArgumentToken>().Select(arg => arg.Value);
            var quotedArguments = arguments.OfType<QuotedCommandArgumentToken>().Select(arg => arg.Value);

            return new ParsedCommand(command, unquotedArguments, quotedArguments);
        }

        return null;
    }
}
