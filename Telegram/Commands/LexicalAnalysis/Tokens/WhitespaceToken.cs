using System.Text.RegularExpressions;

namespace Telegram.Commands.LexicalAnalysis.Tokens;

internal class WhitespaceLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance = new WhitespaceLexemeScanner();

    internal override Regex Pattern => new(@"\s+", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new WhitespaceToken(lexeme);
}

internal class WhitespaceToken : Token
{
    internal WhitespaceToken(string lexeme) : base(lexeme)
    {
    }
}
