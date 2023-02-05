using Telegram.Commands.Tokenization;
using Telegram.Models;

namespace Telegram.Commands;

public abstract class CommandHandler : IUpdateHandler, ISwitchableService<CommandHandler, Command>
{
    readonly CommandTokenizer _tokenizer;

    internal abstract Command Target { get; }

    public CommandHandler(CommandTokenizer tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public bool Matches(Command command) => ((string)command).StartsWith(Target);

    public abstract Task HandleAsync(UpdateContext updateContext, CancellationToken cancellationToken);
}
