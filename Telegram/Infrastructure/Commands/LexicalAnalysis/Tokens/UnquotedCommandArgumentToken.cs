using System.Text.RegularExpressions;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal class UnquotedCommandArgumentToken : CommandToken_
{
    internal class LexemeScanner : LexemeScanner<CommandToken_>
    {
        internal override Regex Pattern => new("^[^/\"\\s][^\\s]*", RegexOptions.Compiled);

        protected override CommandToken_ Token(string lexeme) => new UnquotedCommandArgumentToken(lexeme);
    }

    UnquotedCommandArgumentToken(string lexeme)
        : base(lexeme)
    {
    }
}
