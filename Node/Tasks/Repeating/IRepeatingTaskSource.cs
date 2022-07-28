namespace Node.Tasks.Repeating;

public interface IRepeatingTaskSource : IDisposable
{
    event Action<RepeatingTaskFileAddedEventArgs>? FileAdded;

    void StartListening();
}
