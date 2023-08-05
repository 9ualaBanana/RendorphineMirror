using System.Text.RegularExpressions;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal record QuotedCommandArgumentToken : CommandToken_
{
    internal class LexemeScanner : LexemeScanner<CommandToken_>
    {
        internal override Regex Pattern => new("^\".*?\"", RegexOptions.Compiled);

        protected override CommandToken_ Token(string lexeme) => new QuotedCommandArgumentToken(lexeme);
    }

    QuotedCommandArgumentToken(string lexeme)
        : base(lexeme)
    {
    }

    protected override string Evaluate(string lexeme) => lexeme.Trim('"');
}
