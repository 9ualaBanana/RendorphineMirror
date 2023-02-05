using System.Text.RegularExpressions;
using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands.Tokenization;

internal class TokenizationContext
{
    internal readonly string TokenizerInput;

    internal string TokenizerInputFromPointer => TokenizerInput[Pointer..];

    internal bool TokenizerInputIsExhausted => TokenizerInputFromPointer.Length == 0;

    internal Queue<Token> Tokens => new(_tokens);
    readonly Queue<Token> _tokens;

    internal int Pointer;

    internal TokenizationContext(string tokenizedString)
    {
        TokenizerInput = tokenizedString.Trim();
        _tokens = new();
        Pointer = 0;
    }

    internal Token AddToken(Token token)
    {
        Pointer += token.Lexeme.Length;
        _tokens.Enqueue(token);
        return token;
    }
}
