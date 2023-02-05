using Telegram.Commands.Tokenization.Tokens;

namespace Telegram.Commands;

public class Command : IEquatable<Command>, IEquatable<string>
{
    internal const char Prefix = '/';

    internal readonly string PrefixedCommandText;

    #region Initialization

    //internal static bool TryParseFromMessage(Message commandMessage, [NotNullWhen(true)] out Command? command)
    //    => TryParseFromMessage(commandMessage.Text, out command);

    //internal static bool TryParseFromMessage(string? commandMessage, [NotNullWhen(true)] out Command? command)
    //{
    //    if (CommandToken.ParseAllFrom(commandMessage).First() is CommandToken commandToken)
    //    { command = (string)commandToken; return true; }
    //    else
    //    { command = null; return false; }
    //}

    internal static Command From(CommandToken commandToken) => new(commandToken);

    /// <summary>
    /// Initializes <see cref="Command"/>s from <paramref name="commandToken"/>
    /// unless <paramref name="commandToken"/> contains invalid command characters.
    /// </summary>
    /// <param name="commandToken">
    /// Prefixed/unprefixed command token that represents <see cref="CommandTokenType.Command"/> <see cref="Token"/>.
    /// </param>
    /// <exception cref="FormatException">
    /// <paramref name="commandToken"/> doesn't represent <see cref="CommandTokenType.Command"/> <see cref="Token"/>
    /// </exception>
    protected Command(string commandToken)
    {
        if (CommandLexemeScanner.Instance.Pattern.IsMatch(commandToken))
            PrefixedCommandText = commandToken;
        else throw new FormatException($"{commandToken} doesn't represent a command token.");
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

    public static implicit operator string(Command command) => command.PrefixedCommandText;
    public static implicit operator Command(string commandToken) => new(commandToken);
}
