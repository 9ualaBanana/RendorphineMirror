namespace Node.Tasks.Models;

public record UserTaskInput(UserTaskInputInfo Info) : ITaskInput
{
    public ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        if (task.ExecuteLocally) return Info.Path.AsVTask();

        throw new NotImplementedException();
    }

    public ValueTask Upload()
    {
        throw new NotImplementedException();
    }
}
public record UserTaskOutput(UserTaskOutputInfo Info) : ITaskOutput
{
    public ValueTask Upload(ReceivedTask task, string file)
    {
        if (task.ExecuteLocally)
        {
            Directory.CreateDirectory(Info.Directory);
            File.Move(file, Path.Combine(Info.Directory, Info.FileName), true);
            return ValueTask.CompletedTask;
        }

        throw new NotImplementedException();
    }
}
