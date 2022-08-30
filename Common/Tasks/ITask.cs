namespace Common.Tasks;

public interface ITask : ILoggable
{
    string Id { get; }
}
