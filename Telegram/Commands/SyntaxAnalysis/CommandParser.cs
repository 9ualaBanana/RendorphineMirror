using Telegram.Commands.Tokenization;
using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands.SyntaxAnalysis;

/// <summary>
/// Exposes <see cref="TryParse(string)"/> method that returns <see cref="ParsedCommand"/>
/// if the input <see cref="string"/> was a syntactically correct command.
/// </summary>
public class CommandParser
{
	readonly CommandTokenizer _tokenizer;

	public CommandParser(CommandTokenizer tokenizer)
	{
		_tokenizer = tokenizer;
	}

	/// <summary>
	/// Uses <see cref="CommandTokenizer"/> to make an attempt in constructing <see cref="ParsedCommand"/>
	/// from <paramref name="rawCommand"/> if it is a syntactically correct command.
	/// </summary>
	/// <param name="rawCommand"><see cref="string"/> that should represent a command.</param>
	/// <returns><see cref="ParsedCommand"/> if <paramref name="rawCommand"/> is a syntactically correct command.</returns>
	internal ParsedCommand? TryParse(string rawCommand)
	{
		var tokensEnumerator = _tokenizer.Tokenize(rawCommand).GetEnumerator();

		if (tokensEnumerator.MoveNext() && tokensEnumerator.Current is CommandToken commandToken)
		{
            var command = Command.From(commandToken);

			List<Token> arguments = new();
			while (tokensEnumerator.MoveNext())
				if (tokensEnumerator.Current is UnquotedCommandArgumentToken || tokensEnumerator.Current is QuotedCommandArgumentToken)
					arguments.Add(tokensEnumerator.Current);
			var unquotedArguments = arguments.OfType<UnquotedCommandArgumentToken>().Select(arg => arg.Value);
			var quotedArguments = arguments.OfType<QuotedCommandArgumentToken>().Select(arg => arg.Value);

			return new ParsedCommand(command, unquotedArguments, quotedArguments);
        }

		return null;
	}
}
