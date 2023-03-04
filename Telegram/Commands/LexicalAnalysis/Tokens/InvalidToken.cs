using System.Text.RegularExpressions;

namespace Telegram.Commands.LexicalAnalysis.Tokens;

internal class InvalidLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance => new InvalidLexemeScanner();

    internal override Regex Pattern => new(@"^\S+", RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new InvalidToken(lexeme);
}

internal class InvalidToken : Token
{
    internal InvalidToken(string lexeme) : base(lexeme)
    {
    }
}
