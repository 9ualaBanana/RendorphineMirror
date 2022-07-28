namespace Node.Tasks.Repeating;

// TODO:
public class MPlusRepeatingTaskSource : IRepeatingTaskSource
{
    public event Action<RepeatingTaskFileAddedEventArgs>? FileAdded;

    public void StartListening()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
