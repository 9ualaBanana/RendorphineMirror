namespace Node.Tasks.Repeating;

public interface IRepeatingTaskOutputInfo : ITaskInputOutputInfo
{
    ITaskOutputInfo CreateOutput(string file);
}
