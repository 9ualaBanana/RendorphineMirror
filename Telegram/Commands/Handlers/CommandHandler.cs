using Telegram.Bot;
using Telegram.Commands.SyntacticAnalysis;
using Telegram.Handlers;
using Telegram.Models;

namespace Telegram.Commands.Handlers;

/// <summary>
/// Base class for <see cref="CommandHandler"/>s that should be used to handle <see cref="Target"/> and
/// also provides access to received <see cref="ParsedCommand"/> to its children by calling abstract
/// <see cref="HandleAsync(ParsedCommand, HttpContext)"/>
/// via publicly available <see cref="HandleAsync(HttpContext)"/>.
/// </summary>
public abstract class CommandHandler : UpdateHandler, ISwitchableService<CommandHandler, Command>
{
    readonly CommandParser _parser;

    protected CommandHandler(
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
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
    public bool Matches(Command command)
        => (_receivedCommand = _parser.TryParse(command)) is not null && _receivedCommand.Command == Target;

    ParsedCommand? _receivedCommand;

    public override async Task HandleAsync(HttpContext context)
    {
        if (_receivedCommand is not null)
            await HandleAsync(_receivedCommand, context);
        else
        {
            var exception = new InvalidOperationException(
                $"{nameof(HandleAsync)} can be called only after this {nameof(CommandHandler)} matched the received command.",
                new ArgumentNullException(nameof(_receivedCommand))
                );
            Logger.LogCritical(exception, message: default);
            throw exception;
        }
    }

    protected abstract Task HandleAsync(ParsedCommand receivedCommand, HttpContext context);
}
