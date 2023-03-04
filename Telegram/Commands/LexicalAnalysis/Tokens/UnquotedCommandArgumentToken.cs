using System.Text.RegularExpressions;

namespace Telegram.Commands.LexicalAnalysis.Tokens;

internal class UnquotedCommandArgumentLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance = new UnquotedCommandArgumentLexemeScanner();

    internal override Regex Pattern => new("^[^/\"\\s][^\\s]*", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new UnquotedCommandArgumentToken(lexeme);
}

internal class UnquotedCommandArgumentToken : Token
{
    internal UnquotedCommandArgumentToken(string lexeme) : base(lexeme)
    {
    }
}
