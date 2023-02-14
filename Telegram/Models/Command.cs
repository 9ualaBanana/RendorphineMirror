namespace Telegram.Models;

public abstract class Command : IHandler, ISwitchableService<Command, string>, IEquatable<Command>, IEquatable<string>
{
    internal const char Prefix = '/';

    protected readonly string PrefixedCommandText;

	protected Command(string commandText) => PrefixedCommandText = Prefix + commandText;

    public bool Matches(string prefixedCommandText) => prefixedCommandText.StartsWith(this.PrefixedCommandText);
    
    public abstract Task HandleAsync(HttpContext context, CancellationToken cancellationToken);

    #region Equality

    public static bool operator ==(Command left, Command right) => left.Equals(right);
    public static bool operator !=(Command left, Command right) => !left.Equals(right);
    bool IEquatable<Command>.Equals(Command? otherCommand) => PrefixedCommandText == otherCommand?.PrefixedCommandText;

    public static bool operator ==(Command left, string rightCommandText) => left.Equals(rightCommandText);
    public static bool operator !=(Command left, string rightCommandText) => !left.Equals(rightCommandText);
    bool IEquatable<string>.Equals(string? otherCommandText) => PrefixedCommandText == otherCommandText;
    
    public static implicit operator string(Command command) => command.PrefixedCommandText;

    public override bool Equals(object? obj) => ((IEquatable<Command>)this).Equals(obj as Command);
    public override int GetHashCode() => PrefixedCommandText.GetHashCode();

    #endregion
}
