namespace Common.Tasks;

public interface ITask : ILoggable
{
    string Id { get; }
    bool ExecuteLocally { get; }

    string Action { get; }

    ITaskInputInfo Input { get; }
    ITaskOutputInfo Output { get; }
}
