using System.Text.RegularExpressions;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal class CommandToken : CommandToken_
{
    internal class LexemeScanner : LexemeScanner<CommandToken_>
    {
        internal override Regex Pattern { get; } = new(@$"^{Command.Prefix}[a-z_]{{2,}}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override CommandToken_ Token(string lexeme) => new CommandToken(lexeme);
    }

    CommandToken(string lexeme)
        : base(lexeme)
    {
    }
}
