namespace Telegram.Models;

internal interface IUpdateHandler
{
    Task HandleAsync(CancellationToken cancellationToken);
}
