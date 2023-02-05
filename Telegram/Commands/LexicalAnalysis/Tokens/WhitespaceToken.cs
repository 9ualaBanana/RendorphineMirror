using System.Text.RegularExpressions;

namespace Telegram.Commands.Tokenization.Tokens;

internal class WhitespaceToken : Token
{
    internal WhitespaceToken(string lexeme) : base(lexeme)
    {
    }
}

internal class WhitespaceLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance = new WhitespaceLexemeScanner();

    internal override Regex Pattern => new(@"\s+", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new WhitespaceToken(lexeme);
}
