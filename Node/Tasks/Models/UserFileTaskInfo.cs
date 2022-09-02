namespace Node.Tasks.Models;

public record UserTaskInput(UserTaskInputInfo Info) : ITaskInput
{
    public ValueTask<string> Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
            return Info.Path.AsVTask();

        throw new NotImplementedException();
    }

    public ValueTask Upload()
    {
        throw new NotImplementedException();
    }
}
public record UserTaskOutput(UserTaskOutputInfo Info) : ITaskOutput
{
    public ValueTask Upload(ReceivedTask task, string file, string? postfix)
    {
        if (task.ExecuteLocally || task.Info.LaunchPolicy == TaskPolicy.SameNode)
        {
            var filename = Path.GetFileNameWithoutExtension(Info.FileName) + postfix + Path.GetExtension(Info.FileName);

            Directory.CreateDirectory(Info.Directory);
            File.Copy(file, Path.Combine(Info.Directory, filename), true);
            return ValueTask.CompletedTask;
        }

        throw new NotImplementedException();
    }
}
