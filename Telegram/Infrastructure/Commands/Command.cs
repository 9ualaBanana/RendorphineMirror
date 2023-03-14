using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

namespace Telegram.Infrastructure.Commands;

public class Command : IEquatable<Command>, IEquatable<string>
{
    internal const char Prefix = '/';

    internal readonly string PrefixedCommandText;
    internal readonly string UnprefixedCommandText;

    #region Initialization

    internal static Command From(CommandToken commandToken) => new(commandToken.Lexeme);

    Command(string lexeme)
    {
        (PrefixedCommandText, UnprefixedCommandText) = lexeme.StartsWith(Prefix) ?
            (lexeme, lexeme.TrimStart(Prefix)) : (Prefix + lexeme, lexeme);
    }

    #endregion

    #region Equality

    public static bool operator ==(Command left, Command right) => left.Equals(right);
    public static bool operator !=(Command left, Command right) => !left.Equals(right);
    bool IEquatable<Command>.Equals(Command? otherCommand) => PrefixedCommandText == otherCommand?.PrefixedCommandText;

    public static bool operator ==(Command left, string rightCommandText) => left.Equals(rightCommandText);
    public static bool operator !=(Command left, string rightCommandText) => !left.Equals(rightCommandText);
    bool IEquatable<string>.Equals(string? otherCommandText) => PrefixedCommandText == otherCommandText;


    public override bool Equals(object? obj) => ((IEquatable<Command>)this).Equals(obj as Command);
    public override int GetHashCode() => PrefixedCommandText.GetHashCode();

    #endregion

    #region Conversions

    public static implicit operator string(Command command) => command.PrefixedCommandText;
    public static implicit operator Command(string command) => new(command);

    #endregion
}
