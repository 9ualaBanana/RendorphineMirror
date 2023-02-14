namespace Telegram.Models;

internal interface IHandler
{
    Task HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
