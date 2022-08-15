namespace Node.Tasks.Watching;

public interface IWatchingTaskOutputInfo : ITaskInputOutputInfo
{
    ITaskOutputInfo CreateOutput(string file);
}
