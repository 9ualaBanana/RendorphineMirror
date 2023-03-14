namespace Telegram.Infrastructure;

public interface IHttpContextHandler
{
    Task HandleAsync(HttpContext context);
}
