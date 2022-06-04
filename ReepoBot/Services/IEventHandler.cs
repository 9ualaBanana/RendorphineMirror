namespace ReepoBot.Services;

public interface IEventHandler<TEventArgs>
{
    Task HandleAsync(TEventArgs payload);
}
