namespace Telegram.Models;

internal interface IHttpContextHandler
{
    Task HandleAsync(HttpContext context, CancellationToken cancellationToken);
}
