namespace Common.Tasks;

public interface ITask : ILoggable
{
    string Id { get; }
    bool ExecuteLocally { get; }
}
