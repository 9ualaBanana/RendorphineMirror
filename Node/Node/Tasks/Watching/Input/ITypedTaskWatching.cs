namespace Node.Tasks.Watching.Input;

public interface ITypedTaskWatchingInput
{
    static abstract WatchingTaskInputType Type { get; }
}
