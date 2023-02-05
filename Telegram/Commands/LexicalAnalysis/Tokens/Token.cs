using System.Text.RegularExpressions;

namespace Telegram.Commands.Tokenization.Tokens;

public abstract class Token
{
    internal readonly string Lexeme;
    internal readonly string Value;

    protected Token(string lexeme)
    {
        Lexeme = lexeme;
        Value = Evaluate(lexeme);
    }

    protected virtual string Evaluate(string lexeme) => lexeme;

    #region Casts

    public static implicit operator string(Token commandToken) => commandToken.Value;

    #endregion
}
