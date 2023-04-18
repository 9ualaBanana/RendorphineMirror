using System.Text.RegularExpressions;
using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

internal class WhitespaceToken : CommandToken_
{
    internal class LexemeScanner : LexemeScanner<CommandToken_>
    {
        internal override Regex Pattern => new(@"\s+", RegexOptions.Compiled);

        protected override CommandToken_ Token(string lexeme) => new WhitespaceToken(lexeme);
    }

    WhitespaceToken(string lexeme)
        : base(lexeme)
    {
    }
}
