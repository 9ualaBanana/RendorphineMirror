using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands.Tokenization;

/// <summary>
/// Service that uses registered <see cref="LexemeScanner"/>s to tokenize its input.
/// </summary>
public class CommandTokenizer
{
    readonly IEnumerable<LexemeScanner> _lexemeScanners;

    public CommandTokenizer(IEnumerable<LexemeScanner> lexemeScanners)
    {
        _lexemeScanners = lexemeScanners;
    }

    /// <summary>
    /// Transforms <paramref name="rawCommand"/> into a stream of <see cref="Token"/>s.
    /// </summary>
    internal IEnumerable<Token> Tokenize(string rawCommand)
    {
        var tokenizerInput = new TokenizerInput(rawCommand);

        while (!tokenizerInput.IsExhausted && _lexemeScanners.Any())
            foreach (var lexemeScanner in _lexemeScanners)
                if (lexemeScanner.TryConstructTokenFrom(tokenizerInput) is Token token)
                { yield return token; break; }
    }
}
