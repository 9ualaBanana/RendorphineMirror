namespace Telegram.Models;

public abstract class Command : IUpdateHandler, ISwitchableService<Command, string>, IEquatable<Command>, IEquatable<string>
{
    public readonly string CommandText;

	protected Command(string commandText) => CommandText = commandText;

    public bool Matches(string commandText) => commandText == CommandText;
    
    public abstract Task HandleAsync(UpdateContext updateContext, CancellationToken cancellationToken);

    #region Equality

    public static bool operator ==(Command left, Command right) => left.Equals(right);
    public static bool operator !=(Command left, Command right) => !left.Equals(right);
    bool IEquatable<Command>.Equals(Command? otherCommand) => CommandText == otherCommand?.CommandText;

    public static bool operator ==(Command left, string rightCommandText) => left.Equals(rightCommandText);
    public static bool operator !=(Command left, string rightCommandText) => !left.Equals(rightCommandText);
    bool IEquatable<string>.Equals(string? otherCommandText) => CommandText == otherCommandText;
    
    public static implicit operator string(Command command) => command.CommandText;

    public override bool Equals(object? obj) => ((IEquatable<Command>)this).Equals(obj as Command);
    public override int GetHashCode() => CommandText.GetHashCode();

    #endregion
}
