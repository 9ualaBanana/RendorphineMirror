using Telegram.Infrastructure.Tokenization;

namespace Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

public abstract record CommandToken_ : Token
{
    protected CommandToken_(string lexeme)
        : base(lexeme)
    {
    }
}
