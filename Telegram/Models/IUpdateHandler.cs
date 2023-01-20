namespace Telegram.Models;

internal interface IUpdateHandler
{
    Task HandleAsync(UpdateContext updateContext, CancellationToken cancellationToken);
}
