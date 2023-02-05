using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands.Tokenization;

/// <summary>
/// 
/// </summary>
public class CommandTokenizer
{
    readonly IEnumerable<LexemeScanner> _lexemeScanners;

    public CommandTokenizer(IEnumerable<LexemeScanner> lexemeScanners)
    {
        _lexemeScanners = lexemeScanners;
    }

    internal IEnumerable<Token> Tokenize(string input)
    {
        var tokenizationContext = new TokenizationContext(input);
        while (!tokenizationContext.TokenizerInputIsExhausted && _lexemeScanners.Any())
            foreach (var lexemeScanner in _lexemeScanners)
                if (lexemeScanner.TryGetNextToken(tokenizationContext) is Token token)
                { yield return token; break; }
    }
}
