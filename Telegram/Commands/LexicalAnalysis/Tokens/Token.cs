using System.Text.RegularExpressions;
using Telegram.Commands.LexicalAnalysis;

namespace Telegram.Commands.LexicalAnalysis.Tokens;

/// <summary>
/// Builder responsible for constructing <see cref="Telegram.Commands.LexicalAnalysis.Tokens.Token"/>s of some certain concrete type from scanned lexemes that represent them.
/// </summary>
public abstract class LexemeScanner
{
    /// <summary>
    /// Constructs <see cref="Telegram.Commands.LexicalAnalysis.Tokens.Token"/> of some certain concrete type specific to this <see cref="LexemeScanner"/>.
    /// </summary>
    /// <returns>
    /// Concrete <see cref="Telegram.Commands.LexicalAnalysis.Tokens.Token"/> for construction of which this <see cref="LexemeScanner"/> is responsible
    /// if the current lexeme represents it; <see langword="null"/> otherwise.
    /// </returns>
    internal Token? TryConstructTokenFrom(TokenizerInput tokenizerInput)
        => Pattern.Match(tokenizerInput.Untokenized) is Match match && match.Success ?
        tokenizerInput.Tokenize(Token(match.Value)) : null;

    internal abstract Regex Pattern { get; }

    /// <summary>
    /// Constructs <see cref="Telegram.Commands.LexicalAnalysis.Tokens.Token"/> of concrete type from <paramref name="lexeme"/>.
    /// </summary>
    /// <param name="lexeme">The lexeme to construct <see cref="Token"/> from.</param>
    /// <returns><see cref="Telegram.Commands.LexicalAnalysis.Tokens.Token"/> of concrete type constructed from <paramref name="lexeme"/>.</returns>
    protected abstract Token Token(string lexeme);
}

public abstract class Token
{
    internal readonly string Lexeme;
    internal readonly string Value;

    protected Token(string lexeme)
    {
        Lexeme = lexeme;
        Value = Evaluate(lexeme);
    }

    protected virtual string Evaluate(string lexeme) => lexeme;
}
