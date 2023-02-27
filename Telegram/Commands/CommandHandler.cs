using Telegram.Commands.SyntaxAnalysis;
using Telegram.Models;

namespace Telegram.Commands;

/// <summary>
/// Base class for <see cref="CommandHandler"/>s that should be used to handle <see cref="Target"/> and
/// also provides access to received <see cref="ParsedCommand"/> to its children by calling abstract
/// <see cref="HandleAsync(HttpContext, ParsedCommand, CancellationToken)"/>
/// via publicly available <see cref="HandleAsync(HttpContext, CancellationToken)"/>.
/// </summary>
public abstract class CommandHandler : IHttpContextHandler, ISwitchableService<CommandHandler, Command>
{
    readonly CommandParser _parser;

    protected readonly ILogger Logger;

    internal abstract Command Target { get; }

    protected CommandHandler(CommandParser parser, ILogger logger)
    {
        _parser = parser;
        Logger = logger;
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

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        string receivedMessage = context.GetUpdate().Message!.Text!;
        if (_parser.TryParse(receivedMessage) is ParsedCommand receivedCommand)
            await HandleAsync(context, receivedCommand, cancellationToken);
        else
        {
            string errorMessage =
                "Received message is not a command:\n" +
                $"{receivedMessage}";
            var exception = new ArgumentException(errorMessage, nameof(receivedMessage));
            Logger.LogError(exception, message: default);
            throw exception;
        }
    }

    protected abstract Task HandleAsync(HttpContext context, ParsedCommand receivedCommand, CancellationToken cancellationToken);
}
