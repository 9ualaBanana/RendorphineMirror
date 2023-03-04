using System.Text.RegularExpressions;

namespace Telegram.Commands.LexicalAnalysis.Tokens;

internal class CommandLexemeScanner : LexemeScanner
{
    internal static LexemeScanner Instance => new CommandLexemeScanner();

    internal override Regex Pattern { get; } = new(@$"^{Command.Prefix}[a-z_]{{2,}}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override Token Token(string lexeme) => new CommandToken(lexeme);
}

internal class CommandToken : Token
{
    internal CommandToken(string lexeme) : base(lexeme)
    {
    }
}
