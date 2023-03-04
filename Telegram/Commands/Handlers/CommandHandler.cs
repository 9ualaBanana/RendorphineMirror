using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.Handlers;
using Telegram.Models;

namespace Telegram.Commands.Handlers;

/// <summary>
/// Base class for <see cref="CommandHandler"/>s that should be used to handle <see cref="Target"/> and
/// also provides access to received <see cref="ParsedCommand"/> to its children by calling abstract
/// <see cref="HandleAsync(HttpContext, ParsedCommand)"/>
/// via publicly available <see cref="HandleAsync(HttpContext)"/>.
/// </summary>
public abstract class CommandHandler : UpdateHandler, ISwitchableService<CommandHandler, Command>
{
    readonly CommandParser _parser;

    protected CommandHandler(CommandParser parser, TelegramBot bot, ILogger logger)
        : base(bot, logger)
    {
        _parser = parser;
    }

    internal abstract Command Target { get; }

    /// <summary>
    /// Determines whether this <see cref="CommandHandler"/> is the one that should be used to handle the <paramref name="command"/>.
    /// </summary>
    /// <param name="command">The command that this <see cref="CommandHandler"/> should be able to handle.</param>
    /// <returns>
    /// <see langword="true"/> if this <see cref="CommandHandler"/> is the one that should be used
    /// to handle the <paramref name="command"/>; <see langword="false"/> otherwise.
    /// </returns>
    public bool Matches(Command command) => ((string)command).StartsWith(Target);

    public override async Task HandleAsync(HttpContext context)
    {
        string receivedMessage = context.GetUpdate().Message!.Text!;
        if (_parser.TryParse(receivedMessage) is ParsedCommand receivedCommand)
            await HandleAsync(context, receivedCommand);
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

    protected abstract Task HandleAsync(HttpContext context, ParsedCommand receivedCommand);
}
