using System.Text.RegularExpressions;

namespace Telegram.Commands.Tokenization.Tokens;

internal class InvalidToken : Token
{
    internal InvalidToken(string lexeme) : base(lexeme)
    {
    }
}

internal class InvalidLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance => new InvalidLexemeScanner();

    internal override Regex Pattern => new(@"^\S+", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new InvalidToken(lexeme);
}
