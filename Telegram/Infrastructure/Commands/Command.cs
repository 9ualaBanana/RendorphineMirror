using Telegram.Infrastructure.Commands.LexicalAnalysis.Tokens;

namespace Telegram.Infrastructure.Commands;

public partial class Command : IEquatable<Command>, IEquatable<string>
{
    internal const char Prefix = '/';

    internal readonly string Prefixed;
    internal readonly string Unprefixed;
    internal readonly IEnumerable<string> QuotedArguments;
    internal readonly IEnumerable<string> UnquotedArguments;

    #region Initialization

    Command(CommandToken commandToken, IEnumerable<string>? quotedArguments = null, IEnumerable<string>? unquotedArguments = null)
        : this(commandToken.Lexeme, quotedArguments, unquotedArguments)
    {
    }

    Command(string lexeme, IEnumerable<string>? quotedArguments = null, IEnumerable<string>? unquotedArguments = null)
    {
        Prefixed = lexeme;
        Unprefixed = lexeme.TrimStart(Prefix);
        QuotedArguments = quotedArguments ?? Enumerable.Empty<string>();
        UnquotedArguments = unquotedArguments ?? Enumerable.Empty<string>();
    }

    #endregion

    #region Equality

    public override bool Equals(object? obj) => ((IEquatable<Command>)this).Equals(obj as Command);

    public static bool operator ==(Command left, Command right) => left.Equals(right);
    public static bool operator !=(Command left, Command right) => !left.Equals(right);
    bool IEquatable<Command>.Equals(Command? otherCommand) => Prefixed == otherCommand?.Prefixed;

    public static bool operator ==(Command left, string rightCommandText) => left.Equals(rightCommandText);
    public static bool operator !=(Command left, string rightCommandText) => !left.Equals(rightCommandText);
    bool IEquatable<string>.Equals(string? otherCommandText) => Prefixed == otherCommandText;

    public override int GetHashCode() => Prefixed.GetHashCode();

    #endregion
}
