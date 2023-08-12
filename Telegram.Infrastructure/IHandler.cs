namespace Telegram.Infrastructure;

public interface IHandler
{
    Task HandleAsync();
}
