namespace Telegram.Handlers;

public interface IHttpContextHandler
{
    Task HandleAsync(HttpContext context);
}
