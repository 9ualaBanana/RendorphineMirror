using Telegram.Commands.SyntaxAnalysis;
using Telegram.Models;

namespace Telegram.Commands;

/// <summary>
/// Base class for <see cref="CommandHandler"/>s that should be used to handle <see cref="Target"/> and
/// also provides access to received <see cref="ParsedCommand"/> to its children by calling abstract
/// <see cref="HandleAsync(UpdateContext, ParsedCommand, CancellationToken)"/>
/// via publicly available <see cref="HandleAsync(UpdateContext, CancellationToken)"/>.
/// </summary>
public abstract class CommandHandler : IUpdateHandler, ISwitchableService<CommandHandler, Command>
{
    readonly CommandParser _parser;

    readonly ILogger _logger;

    internal abstract Command Target { get; }

    protected CommandHandler(CommandParser parser, ILogger logger)
    {
        _parser = parser;
        _logger = logger;
    }

    /// <summary>
    /// Determines whether this <see cref="CommandHandler"/> is the one that should be used to handle the <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The command that this <see cref="CommandHandler"/> should be able to handle.</param>
    /// <returns>
    /// <see langword="true"/> if this <see cref="CommandHandler"/> is the one that should be used
    /// to handle the <paramref name="command"/>; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches(Command command) => ((string)command).StartsWith(Target);

    public async Task HandleAsync(UpdateContext updateContext, CancellationToken cancellationToken)
    {
        string receivedMessage = updateContext.Update.Message!.Text!;
        if (_parser.TryParse(receivedMessage) is ParsedCommand receivedCommand)
            await HandleAsync(updateContext, receivedCommand, cancellationToken);
        else
        {
            string errorMessage =
                "Received message is not a command:\n" +
                $"{receivedMessage}";
            var exception = new ArgumentException(errorMessage, nameof(receivedMessage));
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    protected abstract Task HandleAsync(UpdateContext updateContext, ParsedCommand receivedCommand, CancellationToken cancellationToken);
}
