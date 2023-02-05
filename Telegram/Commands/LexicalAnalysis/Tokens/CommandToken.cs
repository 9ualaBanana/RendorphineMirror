using System.Text.RegularExpressions;

namespace Telegram.Commands.Tokenization.Tokens;

internal class CommandToken : Token
{
    internal CommandToken(string lexeme) : base(lexeme)
    {
    }
}

internal class CommandLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance => new CommandLexemeScanner();

    internal override Regex Pattern { get; } = new(@"^/[a-z_]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new CommandToken(lexeme);
}
