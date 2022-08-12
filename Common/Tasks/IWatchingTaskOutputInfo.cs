namespace Common.Tasks;

public interface IWatchingTaskOutputInfo : ITaskInputOutputInfo
{
    ITaskOutputInfo CreateOutput(string file);
}
