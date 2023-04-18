using System.Text.RegularExpressions;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal class InvalidToken : CommandToken_
{
    internal class LexemeScanner : LexemeScanner<CommandToken_>
    {
        internal override Regex Pattern => new(@"^\S+", RegexOptions.Compiled);

        protected override CommandToken_ Token(string lexeme) => new InvalidToken(lexeme);
    }

    InvalidToken(string lexeme)
        : base(lexeme)
    {
    }
}
