using Telegram.Commands.Tokenization;

namespace Telegram.Commands.SyntaxAnalysis;

internal class CommandParser
{
	readonly CommandTokenizer _tokenizer;

	internal CommandParser(CommandTokenizer tokenizer)
	{
		_tokenizer = tokenizer;
	}

	internal void TryParse(string input)
	{
		var tokens = _tokenizer.Tokenize(input);

	}
}
