using System.Text.RegularExpressions;
namespace Telegram.Commands.Tokenization.Tokens;

public abstract class LexemeScanner
{
    internal Token? TryGetNextToken(TokenizationContext tokenizationContext)
        => Pattern.Match(tokenizationContext.TokenizerInputFromPointer) is Match match && match.Success ?
        tokenizationContext.AddToken(Token(match.Value)) : null;

    internal abstract Regex Pattern { get; }

    protected abstract Token Token(string lexeme);
}
